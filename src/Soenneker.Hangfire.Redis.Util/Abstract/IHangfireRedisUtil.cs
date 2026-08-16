using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Hangfire.Redis.Util.Abstract;

/// <summary>
/// A utility library for Hangfire Redis related operations
/// </summary>
public interface IHangfireRedisUtil
{
    /// <summary>
    /// Deletes all hangfire keys safe.
    /// </summary>
    /// <param name="prefix">The prefix.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteAllHangfireKeysSafe(string prefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes Hangfire background-job data in Redis while preserving the specified jobs and Hangfire's recurring-job and server metadata.
    /// </summary>
    /// <param name="prefix">The Hangfire Redis storage prefix.</param>
    /// <param name="preservedJobIds">Job identifiers that must remain in storage.</param>
    /// <param name="batchSize">The maximum number of Redis keys deleted in one operation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of per-job and console Redis keys deleted.</returns>
    Task<long> DeleteAllJobsExcept(string prefix, IReadOnlyCollection<string> preservedJobIds, int batchSize = 500,
        CancellationToken cancellationToken = default);
}
