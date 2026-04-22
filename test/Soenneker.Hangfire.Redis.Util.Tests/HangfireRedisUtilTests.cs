using Soenneker.Hangfire.Redis.Util.Abstract;
using Soenneker.Tests.HostedUnit;

namespace Soenneker.Hangfire.Redis.Util.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class HangfireRedisUtilTests : HostedUnitTest
{
    private readonly IHangfireRedisUtil _util;

    public HangfireRedisUtilTests(Host host) : base(host)
    {
        _util = Resolve<IHangfireRedisUtil>(true);
    }

    [Test]
    public void Default()
    {

    }
}
