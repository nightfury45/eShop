using Microsoft.Extensions.DependencyInjection;

namespace eShop.Admin.API.Infrastructure;

/// <summary>
/// Ensures the adminanalyticsdb schema exists before the app serves traffic. The OLAP store is a new,
/// dashboard-owned database, so EnsureCreated is sufficient here (no shared migration history).
/// </summary>
public sealed class AnalyticsDbInitializer(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
        await db.Database.EnsureCreatedAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
