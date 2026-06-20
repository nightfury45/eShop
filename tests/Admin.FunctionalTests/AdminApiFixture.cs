using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using eShop.Admin.API.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace eShop.Admin.FunctionalTests;

public sealed class AdminApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly IHost _app;

    public IResourceBuilder<PostgresServerResource> Postgres { get; private set; }
    private string _adminConnectionString = string.Empty;
    private string _analyticsConnectionString = string.Empty;

    /// <summary>
    /// In-memory stand-in for Catalog.API so the BFF product endpoints can be exercised without a live
    /// downstream service. Tests seed it before calling the API.
    /// </summary>
    public FakeProductCatalogClient ProductCatalog { get; } = new();

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
                // The OLTP and OLAP contexts are separate databases in production; point them at distinct
                // databases on the one test server so each context's EnsureCreated builds its own schema.
                { "ConnectionStrings:admindb", _adminConnectionString },
                { "ConnectionStrings:adminanalyticsdb", _analyticsConnectionString },
                // The event bus connects on a background thread, so a broker is not required for these
                // tests; a dummy connection string just satisfies the Aspire RabbitMQ client registration.
                { "ConnectionStrings:eventbus", "amqp://localhost:5672" },
                // Satisfy AddDefaultAuthentication; no real token is validated in tests — the
                // AdminAutoAuthorizeMiddleware injects the principal instead.
                { "Identity:Url", "https://identity.test" },
                { "Identity:Audience", "admin" },
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IStartupFilter, AdminAutoAuthorizeStartupFilter>();

            // Replace the HTTP Catalog client with the in-memory fake.
            services.RemoveAll<IProductCatalogClient>();
            services.AddSingleton<IProductCatalogClient>(ProductCatalog);
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
        var baseConnectionString = await Postgres.Resource.GetConnectionStringAsync();
        _adminConnectionString = WithDatabase(baseConnectionString, "admindb");
        _analyticsConnectionString = WithDatabase(baseConnectionString, "adminanalyticsdb");
    }

    private static string WithDatabase(string connectionString, string database) =>
        new NpgsqlConnectionStringBuilder(connectionString) { Database = database }.ConnectionString;
}
