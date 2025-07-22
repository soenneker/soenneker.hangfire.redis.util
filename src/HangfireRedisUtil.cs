using System.Collections.Generic;
using Soenneker.Extensions.ValueTask;
using Soenneker.Hangfire.Redis.Util.Abstract;
using Soenneker.Redis.Util.Abstract;
using Soenneker.Redis.Util.Server.Abstract;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace Soenneker.Hangfire.Redis.Util;

/// <inheritdoc cref="IHangfireRedisUtil"/>
public sealed class HangfireRedisUtil : IHangfireRedisUtil
{
    private readonly IRedisUtil _redisUtil;
    private readonly IRedisServerUtil _redisServerUtil;
    private readonly ILogger<HangfireRedisUtil> _logger;

    public HangfireRedisUtil(IRedisUtil redisUtil, IRedisServerUtil redisServerUtil, ILogger<HangfireRedisUtil> logger)
    {
        _redisUtil = redisUtil;
        _redisServerUtil = redisServerUtil;
        _logger = logger;
    }

    public async Task DeleteAllHangfireKeysSafe(string prefix, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting all Hangfire keys with prefix: {Prefix} ...", prefix);

        var secondaryPrefix = new List<string>
        {
            "job",
            "console"
        };

        foreach (string secondary in secondaryPrefix)
        {
            var fullPrefix = $"{prefix}{secondary}";

            List<RedisKey>? keys = await _redisServerUtil.GetKeysByPrefixList(fullPrefix, cancellationToken).NoSync();

            if (keys is null)
                return;

            _logger.LogInformation("Deleting {count} keys with {Prefix}* ...", keys.Count, prefix);

            foreach (RedisKey key in keys)
            {
                await _redisUtil.Remove(key, false, cancellationToken).NoSync();
            }
        }
    }
}