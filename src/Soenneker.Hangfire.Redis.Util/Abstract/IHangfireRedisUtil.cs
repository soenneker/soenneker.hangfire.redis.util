using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Hangfire.Redis.Util.Abstract;

/// <summary>
/// Removes Hangfire job data directly from Redis while retaining Hangfire infrastructure metadata.
/// </summary>
public interface IHangfireRedisUtil
{
    /// <summary>
    /// Deletes all background-job and console keys under the Hangfire prefix while retaining recurring-job, queue, and server metadata.
    /// </summary>
    /// <param name="prefix">The exact prefix configured for Hangfire Redis storage, including any trailing separator.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes after the targeted Redis keys and job index entries have been removed.</returns>
    Task DeleteAllHangfireKeysSafe(string prefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes Hangfire background-job data in Redis while preserving the specified jobs and Hangfire's recurring-job and server metadata.
    /// </summary>
    /// <param name="prefix">The exact prefix configured for Hangfire Redis storage, including any trailing separator.</param>
    /// <param name="preservedJobIds">Job identifiers that must remain in storage.</param>
    /// <param name="batchSize">The maximum number of Redis keys deleted in one operation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of per-job and console Redis keys deleted.</returns>
    Task<long> DeleteAllJobsExcept(string prefix, IReadOnlyCollection<string> preservedJobIds, int batchSize = 500,
        CancellationToken cancellationToken = default);
}
