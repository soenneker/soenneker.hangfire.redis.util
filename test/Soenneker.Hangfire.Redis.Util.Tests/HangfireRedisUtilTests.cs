using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Soenneker.Hangfire.Redis.Util.Abstract;
using Soenneker.Redis.Client.Abstract;
using Soenneker.Tests.HostedUnit;
using StackExchange.Redis;

namespace Soenneker.Hangfire.Redis.Util.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class HangfireRedisUtilTests : HostedUnitTest
{
    private readonly IHangfireRedisUtil _util;
    private readonly IRedisClient _redisClient;

    public HangfireRedisUtilTests(Host host) : base(host)
    {
        _util = Resolve<IHangfireRedisUtil>(true);
        _redisClient = Resolve<IRedisClient>(true);
    }

    [Test]
    public async Task DeleteAllJobsExcept_should_preserve_selected_job_and_metadata()
    {
        const string preservedJobId = "preserved";
        const string deletedJobId = "deleted";
        string prefix = $"{{hangfire-util-test-{Guid.NewGuid():N}}}:";
        ConnectionMultiplexer connection = await _redisClient.Get(CancellationToken.None);
        IDatabase database = connection.GetDatabase();

        RedisKey[] keys =
        [
            $"{prefix}job:{preservedJobId}", $"{prefix}job:{preservedJobId}:state", $"{prefix}job:{deletedJobId}",
            $"{prefix}job:{deletedJobId}:state", $"{prefix}job:{deletedJobId}:history", $"{prefix}console:{preservedJobId}",
            $"{prefix}console:{deletedJobId}", $"{prefix}succeeded", $"{prefix}deleted", $"{prefix}processing", $"{prefix}failed",
            $"{prefix}awaiting", $"{prefix}schedule", $"{prefix}queue:default", $"{prefix}queue:default:dequeued",
            $"{prefix}queue:default:dequeued:lock", $"{prefix}queues", $"{prefix}recurring-jobs", $"{prefix}server:test"
        ];

        try
        {
            await database.HashSetAsync(keys[0], "State", "Processing");
            await database.HashSetAsync(keys[1], "State", "Processing");
            await database.HashSetAsync(keys[2], "State", "Succeeded");
            await database.HashSetAsync(keys[3], "State", "Succeeded");
            await database.ListRightPushAsync(keys[4], "history");
            await database.StringSetAsync(keys[5], "preserved console");
            await database.StringSetAsync(keys[6], "deleted console");
            await database.ListRightPushAsync(keys[7], [deletedJobId, preservedJobId]);
            await database.ListRightPushAsync(keys[8], deletedJobId);
            await database.SortedSetAddAsync(keys[9], [new SortedSetEntry(deletedJobId, 1), new SortedSetEntry(preservedJobId, 2)]);
            await database.SortedSetAddAsync(keys[10], deletedJobId, 1);
            await database.SortedSetAddAsync(keys[11], deletedJobId, 1);
            await database.SortedSetAddAsync(keys[12],
                [new SortedSetEntry($"default:{deletedJobId}", 1), new SortedSetEntry($"default:{preservedJobId}", 2)]);
            await database.ListRightPushAsync(keys[13], [deletedJobId, preservedJobId]);
            await database.ListRightPushAsync(keys[14], [deletedJobId, preservedJobId]);
            await database.StringSetAsync(keys[15], "lock");
            await database.SetAddAsync(keys[16], "default");
            await database.SortedSetAddAsync(keys[17], "recurring", 1);
            await database.HashSetAsync(keys[18], "StartedAt", "now");

            long deletedKeys = await _util.DeleteAllJobsExcept(prefix, [preservedJobId], cancellationToken: CancellationToken.None);

            deletedKeys.Should().Be(4);
            (await database.KeyExistsAsync(keys[0])).Should().BeTrue();
            (await database.KeyExistsAsync(keys[1])).Should().BeTrue();
            (await database.KeyExistsAsync(keys[2])).Should().BeFalse();
            (await database.KeyExistsAsync(keys[3])).Should().BeFalse();
            (await database.KeyExistsAsync(keys[4])).Should().BeFalse();
            (await database.KeyExistsAsync(keys[5])).Should().BeTrue();
            (await database.KeyExistsAsync(keys[6])).Should().BeFalse();
            (await database.ListRangeAsync(keys[7])).Should().ContainSingle().Which.ToString().Should().Be(preservedJobId);
            (await database.SortedSetRangeByRankAsync(keys[9])).Should().ContainSingle().Which.ToString().Should().Be(preservedJobId);
            (await database.SortedSetRangeByRankAsync(keys[12])).Should().ContainSingle().Which.ToString().Should().Be($"default:{preservedJobId}");
            (await database.ListRangeAsync(keys[13])).Should().ContainSingle().Which.ToString().Should().Be(preservedJobId);
            (await database.ListRangeAsync(keys[14])).Should().ContainSingle().Which.ToString().Should().Be(preservedJobId);
            (await database.KeyExistsAsync(keys[15])).Should().BeTrue();
            (await database.KeyExistsAsync(keys[16])).Should().BeTrue();
            (await database.KeyExistsAsync(keys[17])).Should().BeTrue();
            (await database.KeyExistsAsync(keys[18])).Should().BeTrue();
        }
        finally
        {
            await database.KeyDeleteAsync(keys);
        }

    }
}
