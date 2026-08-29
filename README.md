[![](https://img.shields.io/nuget/v/soenneker.hangfire.redis.util.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.hangfire.redis.util/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.hangfire.redis.util/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.hangfire.redis.util/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.hangfire.redis.util.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.hangfire.redis.util/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.hangfire.redis.util/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.hangfire.redis.util/actions/workflows/codeql.yml)

# Soenneker.Hangfire.Redis.Util

A utility library for Hangfire Redis related operations.

## Install

```bash
dotnet add package Soenneker.Hangfire.Redis.Util
```

## Quick start

```csharp
using Soenneker.Hangfire.Redis.Util.Registrars;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
var result = services.AddHangfireRedisUtilAsSingleton();
```

Adds `IHangfireRedisUtil` as a singleton service.

## What you get

- `IHangfireRedisUtil` — A utility library for Hangfire Redis related operations.
- `HangfireRedisUtilRegistrar` — A utility library for Hangfire Redis related operations.

## API at a glance

| API | What it does | Result / important behavior |
| --- | --- | --- |
| `IHangfireRedisUtil.DeleteAllJobsExcept(prefix, preservedJobIds, batchSize, cancellationToken)` | Deletes Hangfire background-job data in Redis while preserving the specified jobs and Hangfire's recurring-job and server metadata. | The number of per-job and console Redis keys deleted. |
| `HangfireRedisUtilRegistrar.AddHangfireRedisUtilAsSingleton(services)` | Adds `IHangfireRedisUtil` as a singleton service. | The same service collection, so additional registrations can be chained. |
| `HangfireRedisUtilRegistrar.AddHangfireRedisUtilAsScoped(services)` | Adds `IHangfireRedisUtil` as a scoped service. | The same service collection, so additional registrations can be chained. |

## Practical notes

- Cancellation stops pending work; it does not undo work that has already completed.
