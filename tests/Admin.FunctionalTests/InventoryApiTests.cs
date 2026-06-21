using System.Net;
using System.Net.Http.Json;
using eShop.Admin.API.Infrastructure;
using eShop.Admin.API.IntegrationEvents.Events;
using eShop.Admin.API.Services;
using eShop.IntegrationEventLogEF;
using Microsoft.Extensions.DependencyInjection;

namespace eShop.Admin.FunctionalTests;

public sealed class InventoryApiTests : IClassFixture<AdminApiFixture>
{
    private readonly AdminApiFixture _fixture;

    public InventoryApiTests(AdminApiFixture fixture)
    {
        _fixture = fixture;
    }

    private static CatalogItemDetail Item(int id, string name, decimal price, int stock, int restock = 10) =>
        new(id, name, $"{name} description", price, CatalogTypeId: 1, CatalogBrandId: 1,
            AvailableStock: stock, RestockThreshold: restock, MaxStockThreshold: 100, PictureFileName: null,
            OnReorder: false);

    private static HttpRequestMessage AdminRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add(AdminAutoAuthorizeMiddleware.AdminHeader, "true");
        return request;
    }

    [Fact]
    public async Task InventoryReturnsUnauthorizedWithoutAdminPrincipal()
    {
        var httpClient = _fixture.CreateDefaultClient();

        var response = await httpClient.GetAsync("/api/admin/inventory", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InventoryReturnsKpisAndRowsForAuthorizedAdmin()
    {
        _fixture.ProductCatalog.Seed(
            Item(301, "Aero Runner", 100m, 50, restock: 10),
            Item(302, "Merino Knit", 70m, 0, restock: 10));

        var httpClient = _fixture.CreateDefaultClient();
        var response = await httpClient.SendAsync(
            AdminRequest(HttpMethod.Get, "/api/admin/inventory"), TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<InventoryResult>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.Summary.TotalSkus >= 2);
        Assert.Contains(result.Items, i => i.Id == 301 && i.Status == "Active" && i.Sku == "SKU-0301");
        Assert.Contains(result.Items, i => i.Id == 302 && i.Status == "OutOfStock");
    }

    [Fact]
    public async Task AdjustUpdatesCatalogWritesAuditAndOutboxEvent()
    {
        _fixture.ProductCatalog.Seed(Item(401, "Field Tote", 89m, 38, restock: 40));

        var httpClient = _fixture.CreateDefaultClient();
        var request = AdminRequest(HttpMethod.Post, "/api/admin/inventory/401/adjust");
        request.Content = JsonContent.Create(new StockAdjustment(NewOnHand: 12, Reason: "Cycle count correction"));

        var response = await httpClient.SendAsync(request, TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<InventoryItem>(TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(12, updated.OnHand);

        // Catalog received the new on-hand and the recomputed reorder flag (12 <= 40).
        var current = _fixture.ProductCatalog.Current(401);
        Assert.Equal(12, current.AvailableStock);
        Assert.True(current.OnReorder);

        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

        var audit = db.InventoryAudits.Single(a => a.ProductId == 401);
        Assert.Equal(38, audit.OldOnHand);
        Assert.Equal(12, audit.NewOnHand);
        Assert.Equal("Cycle count correction", audit.Reason);

        // The outbox holds the event, persisted in the same transaction as the audit row.
        Assert.Contains(
            db.Set<IntegrationEventLogEntry>().ToList(),
            e => e.EventTypeShortName == nameof(AdminInventoryStockUpdatedIntegrationEvent));
    }

    [Fact]
    public async Task AdjustReturnsNotFoundForMissingProduct()
    {
        var httpClient = _fixture.CreateDefaultClient();
        var request = AdminRequest(HttpMethod.Post, "/api/admin/inventory/999999/adjust");
        request.Content = JsonContent.Create(new StockAdjustment(NewOnHand: 5, Reason: "x"));

        var response = await httpClient.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AdjustRejectsNegativeQuantityAndEmptyReason()
    {
        _fixture.ProductCatalog.Seed(Item(501, "Studio Mug", 20m, 100));

        var httpClient = _fixture.CreateDefaultClient();
        var request = AdminRequest(HttpMethod.Post, "/api/admin/inventory/501/adjust");
        request.Content = JsonContent.Create(new StockAdjustment(NewOnHand: -5, Reason: ""));

        var response = await httpClient.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
