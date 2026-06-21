using eShop.Admin.API.IntegrationEvents;
using eShop.Admin.API.IntegrationEvents.EventHandling;
using eShop.Admin.API.IntegrationEvents.Events;
using eShop.Admin.API.Services;
using eShop.IntegrationEventLogEF.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;

public static class Extensions
{
    /// <summary>
    /// Registers Admin Dashboard BFF application services: the dashboard-owned OLTP database (admindb),
    /// the read-optimized OLAP store (adminanalyticsdb), inbound JWT auth + admin policy, the eShop event
    /// bus with the analytics consumers, and the Catalog enrichment client.
    /// </summary>
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<AdminDbContext>("admindb");
        builder.AddNpgsqlDbContext<AnalyticsDbContext>("adminanalyticsdb");
        builder.Services.AddHostedService<AdminDbInitializer>();
        builder.Services.AddHostedService<AnalyticsDbInitializer>();

        // Validate JWTs issued by Identity.API (reads Identity:Url + Identity:Audience).
        builder.AddDefaultAuthentication();

        // Identity.API's ProfileService emits roles as "role" claims; map them for RequireRole.
        builder.Services.Configure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            options => options.TokenValidationParameters.RoleClaimType = "role");

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("Admin", policy => policy.RequireRole("Administrator"));

        // Consume eShop integration events to populate the analytics facts (own queue via EventBus config).
        builder.AddRabbitMqEventBus("eventbus")
            .AddSubscription<OrderStatusChangedToPaidIntegrationEvent, OrderStatusChangedToPaidIntegrationEventHandler>()
            .AddSubscription<OrderStatusChangedToCancelledIntegrationEvent, OrderStatusChangedToCancelledIntegrationEventHandler>()
            .AddSubscription<ProductPriceChangedIntegrationEvent, ProductPriceChangedIntegrationEventHandler>();

        builder.Services.AddScoped<IAnalyticsRecorder, AnalyticsRecorder>();
        builder.Services.AddScoped<IAnalyticsQueries, AnalyticsQueries>();

        // Catalog is anonymous for reads — no token propagation needed for enrichment.
        builder.Services.AddHttpClient<ICatalogEnricher, CatalogEnricher>(client =>
            client.BaseAddress = new Uri("https+http://catalog-api"));

        // Transactional outbox (admindb): outbound admin-action events are persisted atomically with the
        // dashboard's audit rows, then published by AdminIntegrationEventService.
        builder.Services.AddTransient<IIntegrationEventLogService, IntegrationEventLogService<AdminDbContext>>();
        builder.Services.AddTransient<IAdminIntegrationEventService, AdminIntegrationEventService>();

        // Product-management aggregation: reads/writes Catalog.API and records dashboard audit + events.
        builder.Services.AddHttpClient<IProductCatalogClient, ProductCatalogClient>(client =>
            client.BaseAddress = new Uri("https+http://catalog-api"));
        builder.Services.AddScoped<IProductCatalogService, ProductCatalogService>();

        // Inventory aggregation + stock adjustment (shares the Catalog client and the outbox).
        builder.Services.AddScoped<IInventoryService, InventoryService>();
    }
}
