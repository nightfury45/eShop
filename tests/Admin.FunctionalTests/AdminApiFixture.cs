using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.AspNetCore.Mvc.Testing;

namespace eShop.Admin.FunctionalTests;

public sealed class AdminApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly IHost _app;

    public IResourceBuilder<PostgresServerResource> Postgres { get; private set; }
    private string _postgresConnectionString = string.Empty;

    public AdminApiFixture()
    {
        var options = new DistributedApplicationOptions
        {
            AssemblyName = typeof(AdminApiFixture).Assembly.FullName,
            DisableDashboard = true,
        };
        var appBuilder = DistributedApplication.CreateBuilder(options);
        Postgres = appBuilder.AddPostgres("admindb")
            .WithImage("ankane/pgvector")
            .WithImageTag("latest");
        _app = appBuilder.Build();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                { $"ConnectionStrings:{Postgres.Resource.Name}", _postgresConnectionString },
            });
        });
        return base.CreateHost(builder);
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _app.StopAsync();
        if (_app is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
        else
        {
            _app.Dispose();
        }
    }

    public async ValueTask InitializeAsync()
    {
        await _app.StartAsync();
        _postgresConnectionString = await Postgres.Resource.GetConnectionStringAsync();
    }
}
