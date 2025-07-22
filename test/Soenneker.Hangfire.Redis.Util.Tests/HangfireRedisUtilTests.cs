using Soenneker.Hangfire.Redis.Util.Abstract;
using Soenneker.Tests.FixturedUnit;
using Xunit;

namespace Soenneker.Hangfire.Redis.Util.Tests;

[Collection("Collection")]
public sealed class HangfireRedisUtilTests : FixturedUnitTest
{
    private readonly IHangfireRedisUtil _util;

    public HangfireRedisUtilTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _util = Resolve<IHangfireRedisUtil>(true);
    }

    [Fact]
    public void Default()
    {

    }
}
