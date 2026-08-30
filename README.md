[![](https://img.shields.io/nuget/v/soenneker.hangfire.redis.util.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.hangfire.redis.util/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.hangfire.redis.util/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.hangfire.redis.util/actions/workflows/publish-package.yml)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.hangfire.redis.util/build-and-test.yml?style=for-the-badge&label=build)](https://github.com/soenneker/soenneker.hangfire.redis.util/actions/workflows/build-and-test.yml)
[![](https://img.shields.io/nuget/dt/soenneker.hangfire.redis.util.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.hangfire.redis.util/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.hangfire.redis.util/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.hangfire.redis.util/actions/workflows/codeql.yml)

# Soenneker.Hangfire.Redis.Util

Provides destructive maintenance operations for removing Hangfire background-job data directly from Redis while preserving selected jobs and Hangfire infrastructure metadata.

## Installation

```bash
dotnet add package Soenneker.Hangfire.Redis.Util
```

## Configuration and registration

```json
{
  "Redis": {
    "ConnectionString": "redis.example.com:6379,password=..."
  }
}
```

```csharp
using Soenneker.Hangfire.Redis.Util.Registrars;

services.AddHangfireRedisUtilAsScoped();
```

The scoped utility uses the long-lived Redis connections registered by the underlying Redis packages; disposing the utility scope does not tear down the shared connection.

## Delete jobs while preserving specific IDs

```csharp
using Soenneker.Hangfire.Redis.Util.Abstract;

long deletedKeyCount = await redisUtil.DeleteAllJobsExcept(
    prefix: "{hangfire}:",
    preservedJobIds: [importantJobId, anotherJobId],
    batchSize: 500,
    cancellationToken);
```

The prefix must exactly match the prefix configured for Hangfire Redis storage, including its trailing separator. The method removes non-preserved `job:` and `console:` keys and removes non-preserved entries from succeeded, deleted, processing, failed, awaiting, scheduled, and queue indexes. Recurring-job definitions, queue metadata, locks, and server metadata are retained.

To remove every background job without preserving IDs:

```csharp
await redisUtil.DeleteAllHangfireKeysSafe("{hangfire}:", cancellationToken);
```

“Safe” means Hangfire infrastructure metadata is retained; the operation still permanently deletes all job data. Stop or quiesce Hangfire workers before running either method. Cleanup spans multiple Redis keys and is not one atomic transaction, so cancellation or a Redis failure can leave a partially cleaned data set. Back up production Redis data and verify the prefix before use.

`DeleteAllJobsExcept()` returns only the number of per-job and console keys deleted; removed index entries are not included in that count.
