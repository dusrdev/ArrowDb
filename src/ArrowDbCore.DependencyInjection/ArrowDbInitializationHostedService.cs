using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ArrowDbCore.DependencyInjection;

/// <summary>
/// Primes the registered <see cref="IArrowDbProvider"/> during host startup.
/// </summary>
internal sealed class ArrowDbInitializationHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public ArrowDbInitializationHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        IArrowDbProvider provider = _serviceProvider.GetRequiredService<IArrowDbProvider>();
        await provider.GetAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
