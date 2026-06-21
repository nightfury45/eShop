using Microsoft.Extensions.DependencyInjection;

namespace eShop.Admin.API.Infrastructure;

/// <summary>
/// Ensures the admindb (OLTP) schema exists before the app serves traffic. Like the analytics store,
/// admindb is a new, dashboard-owned database with no shared migration history, so EnsureCreated is
/// sufficient here.
/// </summary>
public sealed class AdminDbInitializer(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
        await db.Database.EnsureCreatedAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
