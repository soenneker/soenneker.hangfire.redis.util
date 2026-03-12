using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Hangfire.Redis.Util.Abstract;

/// <summary>
/// A utility library for Hangfire Redis related operations
/// </summary>
public interface IHangfireRedisUtil
{
    Task DeleteAllHangfireKeysSafe(string prefix, CancellationToken cancellationToken = default);
}