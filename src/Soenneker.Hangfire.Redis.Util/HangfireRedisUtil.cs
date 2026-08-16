using Microsoft.Extensions.Logging;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.Hangfire.Redis.Util.Abstract;
using Soenneker.Redis.Client.Abstract;
using Soenneker.Redis.Util.Server.Abstract;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Hangfire.Redis.Util;

/// <inheritdoc cref="IHangfireRedisUtil"/>
public sealed class HangfireRedisUtil : IHangfireRedisUtil
{
    private readonly IRedisServerUtil _redisServerUtil;
    private readonly IRedisClient _redisClient;
    private readonly ILogger<HangfireRedisUtil> _logger;

    public HangfireRedisUtil(IRedisServerUtil redisServerUtil, IRedisClient redisClient, ILogger<HangfireRedisUtil> logger)
    {
        _redisServerUtil = redisServerUtil;
        _redisClient = redisClient;
        _logger = logger;
    }

    public async Task DeleteAllHangfireKeysSafe(string prefix, CancellationToken cancellationToken = default)
    {
        _ = await DeleteAllJobsExcept(prefix, [], cancellationToken: cancellationToken).NoSync();
    }

    public async Task<long> DeleteAllJobsExcept(string prefix, IReadOnlyCollection<string> preservedJobIds, int batchSize = 500,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        ArgumentNullException.ThrowIfNull(preservedJobIds);
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);

        var preservedSet = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in preservedJobIds)
        {
            if (!string.IsNullOrWhiteSpace(id))
                preservedSet.Add(id);
        }

        string[] preserved = new string[preservedSet.Count];
        preservedSet.CopyTo(preserved);
        ConnectionMultiplexer connection = await _redisClient.Get(cancellationToken).NoSync();
        IDatabase database = connection.GetDatabase();

        _logger.LogInformation("Deleting Hangfire jobs with prefix {Prefix}, preserving {PreservedCount} jobs", prefix, preserved.Length);

        long removedIndexEntries = await FilterJobIndexes(database, prefix, preserved, cancellationToken).NoSync();
        removedIndexEntries += await FilterQueues(database, prefix, preserved, cancellationToken).NoSync();

        string jobKeyPrefix = $"{prefix}job:";
        long deletedKeys = await _redisServerUtil.RemoveByScan(jobKeyPrefix,
            key => !IsPreservedKey(key.ToString(), jobKeyPrefix.Length, preserved), batchSize, cancellationToken).NoSync();

        string consoleKeyPrefix = $"{prefix}console";
        deletedKeys += await _redisServerUtil.RemoveByScan(consoleKeyPrefix,
            key => !IsPreservedKey(key.ToString(), consoleKeyPrefix.Length, preserved), batchSize, cancellationToken).NoSync();

        _logger.LogInformation("Deleted {DeletedKeyCount} Hangfire job keys and removed {RemovedIndexEntryCount} queue/state entries with prefix {Prefix}",
            deletedKeys, removedIndexEntries, prefix);

        return deletedKeys;
    }

    private static async Task<long> FilterJobIndexes(IDatabase database, string prefix, IReadOnlyCollection<string> preservedJobIds,
        CancellationToken cancellationToken)
    {
        RedisValue[] exactCandidates = ToRedisValues(preservedJobIds);
        RedisValue[] queues = await database.SetMembersAsync($"{prefix}queues").WaitAsync(cancellationToken).NoSync();
        RedisValue[] scheduleCandidates = BuildScheduleCandidates(exactCandidates, queues);

        Task<long> succeeded = FilterList(database, $"{prefix}succeeded", exactCandidates, cancellationToken);
        Task<long> deleted = FilterList(database, $"{prefix}deleted", exactCandidates, cancellationToken);
        Task<long> processing = FilterSortedSet(database, $"{prefix}processing", exactCandidates, cancellationToken);
        Task<long> failed = FilterSortedSet(database, $"{prefix}failed", exactCandidates, cancellationToken);
        Task<long> awaiting = FilterSortedSet(database, $"{prefix}awaiting", exactCandidates, cancellationToken);
        Task<long> schedule = FilterSortedSet(database, $"{prefix}schedule", scheduleCandidates, cancellationToken);

        return await succeeded.NoSync() + await deleted.NoSync() + await processing.NoSync() + await failed.NoSync() +
               await awaiting.NoSync() + await schedule.NoSync();
    }

    private async Task<long> FilterQueues(IDatabase database, string prefix, IReadOnlyCollection<string> preservedJobIds,
        CancellationToken cancellationToken)
    {
        RedisValue[] candidates = ToRedisValues(preservedJobIds);
        IAsyncEnumerable<RedisKey>? keys = await _redisServerUtil.GetKeysByPrefix($"{prefix}queue:", cancellationToken).NoSync();
        if (keys is null)
            return 0;

        var tasks = new List<Task<long>>();
        await foreach (RedisKey key in keys.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (key.ToString().EndsWith(":lock", StringComparison.Ordinal))
                continue;

            tasks.Add(FilterList(database, key, candidates, cancellationToken));
        }

        if (tasks.Count == 0)
            return 0;

        long removed = 0;
        foreach (Task<long> task in tasks)
            removed += await task.WaitAsync(cancellationToken).NoSync();

        return removed;
    }

    private static async Task<long> FilterList(IDatabase database, RedisKey key, RedisValue[] candidates,
        CancellationToken cancellationToken)
    {
        long count = await database.ListLengthAsync(key).WaitAsync(cancellationToken).NoSync();
        if (count == 0)
            return 0;

        var retained = new List<(long Position, RedisValue Value)>(candidates.Length);
        foreach (RedisValue candidate in candidates)
        {
            long? position = await database.ListPositionAsync(key, candidate).WaitAsync(cancellationToken).NoSync();
            if (position.HasValue)
                retained.Add((position.Value, candidate));
        }

        retained.Sort(static (left, right) => left.Position.CompareTo(right.Position));
        ITransaction transaction = database.CreateTransaction();
        _ = transaction.KeyDeleteAsync(key);
        if (retained.Count > 0)
        {
            var values = new RedisValue[retained.Count];
            for (var i = 0; i < retained.Count; i++)
                values[i] = retained[i].Value;

            _ = transaction.ListRightPushAsync(key, values);
        }

        bool committed = await transaction.ExecuteAsync().WaitAsync(cancellationToken).NoSync();
        if (!committed)
            throw new InvalidOperationException($"Could not update Hangfire list '{key}'.");

        return count - retained.Count;
    }

    private static async Task<long> FilterSortedSet(IDatabase database, RedisKey key, RedisValue[] candidates,
        CancellationToken cancellationToken)
    {
        long count = await database.SortedSetLengthAsync(key).WaitAsync(cancellationToken).NoSync();
        if (count == 0)
            return 0;

        var retained = new List<SortedSetEntry>(candidates.Length);
        foreach (RedisValue candidate in candidates)
        {
            double? score = await database.SortedSetScoreAsync(key, candidate).WaitAsync(cancellationToken).NoSync();
            if (score.HasValue)
                retained.Add(new SortedSetEntry(candidate, score.Value));
        }

        ITransaction transaction = database.CreateTransaction();
        _ = transaction.KeyDeleteAsync(key);
        if (retained.Count > 0)
            _ = transaction.SortedSetAddAsync(key, retained.ToArray());

        bool committed = await transaction.ExecuteAsync().WaitAsync(cancellationToken).NoSync();
        if (!committed)
            throw new InvalidOperationException($"Could not update Hangfire sorted set '{key}'.");

        return count - retained.Count;
    }

    private static RedisValue[] ToRedisValues(IReadOnlyCollection<string> values)
    {
        var result = new RedisValue[values.Count];
        var index = 0;
        foreach (string value in values)
            result[index++] = value;

        return result;
    }

    private static RedisValue[] BuildScheduleCandidates(RedisValue[] jobIds, RedisValue[] queues)
    {
        if (jobIds.Length == 0 || queues.Length == 0)
            return jobIds;

        var result = new RedisValue[jobIds.Length * (queues.Length + 1)];
        Array.Copy(jobIds, result, jobIds.Length);
        var index = jobIds.Length;

        foreach (RedisValue queue in queues)
        {
            string queuePrefix = queue + ":";
            foreach (RedisValue jobId in jobIds)
                result[index++] = queuePrefix + jobId;
        }

        return result;
    }

    private static bool IsPreservedKey(string key, int prefixLength, IReadOnlyCollection<string> preservedJobIds)
    {
        ReadOnlySpan<char> suffix = key.AsSpan(prefixLength);

        foreach (string jobId in preservedJobIds)
        {
            ReadOnlySpan<char> id = jobId.AsSpan();
            var searchFrom = 0;

            while (searchFrom <= suffix.Length - id.Length)
            {
                int relativeIndex = suffix[searchFrom..].IndexOf(id, StringComparison.Ordinal);
                if (relativeIndex < 0)
                    break;

                int index = searchFrom + relativeIndex;
                bool startsSegment = index == 0 || suffix[index - 1] == ':';
                int end = index + id.Length;
                bool endsSegment = end == suffix.Length || suffix[end] == ':';
                if (startsSegment && endsSegment)
                    return true;

                searchFrom = index + 1;
            }
        }

        return false;
    }
}
