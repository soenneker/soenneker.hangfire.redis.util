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
}