using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Hangfire.Redis.Util.Abstract;
using Soenneker.Redis.Util.Server.Registrars;

namespace Soenneker.Hangfire.Redis.Util.Registrars;

/// <summary>
/// A utility library for Hangfire Redis related operations
/// </summary>
public static class HangfireRedisUtilRegistrar
{
    /// <summary>
    /// Adds <see cref="IHangfireRedisUtil"/> as a singleton service. <para/>
    /// </summary>
    public static IServiceCollection AddHangfireRedisUtilAsSingleton(this IServiceCollection services)
    {
        services.AddRedisServerUtilAsSingleton().TryAddSingleton<IHangfireRedisUtil, HangfireRedisUtil>();

        return services;
    }

    /// <summary>
    /// Adds <see cref="IHangfireRedisUtil"/> as a scoped service. <para/>
    /// </summary>
    public static IServiceCollection AddHangfireRedisUtilAsScoped(this IServiceCollection services)
    {
        services.AddRedisServerUtilAsScoped().TryAddScoped<IHangfireRedisUtil, HangfireRedisUtil>();

        return services;
    }
}
