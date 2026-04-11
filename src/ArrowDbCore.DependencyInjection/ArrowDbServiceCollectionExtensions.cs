using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ArrowDbCore.DependencyInjection;

/// <summary>
/// Provides optional service registration helpers for ArrowDb dependency injection integration.
/// </summary>
public static class ArrowDbServiceCollectionExtensions
{
    /// <summary>
    /// Adds a hosted service that primes the registered <see cref="IArrowDbProvider"/> during host startup.
    /// </summary>
    public static IServiceCollection AddArrowDbInitialization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IHostedService, ArrowDbInitializationHostedService>();
        return services;
    }
}
