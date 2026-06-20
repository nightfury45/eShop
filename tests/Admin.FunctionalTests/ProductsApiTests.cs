using System.Net;
using System.Net.Http.Json;
using eShop.Admin.API.Infrastructure;
using eShop.Admin.API.Services;
using Microsoft.Extensions.DependencyInjection;

namespace eShop.Admin.FunctionalTests;

public sealed class ProductsApiTests : IClassFixture<AdminApiFixture>
{
    private readonly AdminApiFixture _fixture;

    public ProductsApiTests(AdminApiFixture fixture)
    {
        _fixture = fixture;
    }

    private static CatalogItemDetail Item(int id, string name, decimal price, int stock, int restock = 10) =>
        new(id, name, $"{name} description", price, CatalogTypeId: 1, CatalogBrandId: 1,
            AvailableStock: stock, RestockThreshold: restock, MaxStockThreshold: 100, PictureFileName: null);

    [Fact]
    public async Task ListReturnsUnauthorizedWithoutAdminPrincipal()
    {
        var httpClient = _fixture.CreateDefaultClient();

        var response = await httpClient.GetAsync("/api/admin/products", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListReturnsMappedProductsForAuthorizedAdmin()
    {
        _fixture.ProductCatalog.Seed(
            Item(101, "Aero Runner", 129m, 412),
            Item(102, "Merino Knit", 74m, 0));

        var httpClient = _fixture.CreateDefaultClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/products");
        request.Headers.Add(AdminAutoAuthorizeMiddleware.AdminHeader, "true");

        var response = await httpClient.SendAsync(request, TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AdminProductsResult>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        var runner = result.Items.Single(p => p.Id == 101);
        Assert.Equal("Footwear", runner.Category);
        Assert.Equal("SKU-0101", runner.Sku);
        Assert.Equal("Active", runner.Status);
        Assert.Equal("OutOfStock", result.Items.Single(p => p.Id == 102).Status);
        Assert.Contains(result.Categories, c => c.Name == "Footwear");
    }

    [Fact]
    public async Task UpdatePersistsAuditRowForAuthorizedAdmin()
    {
        _fixture.ProductCatalog.Seed(Item(201, "Field Tote", 89m, 38));

        var httpClient = _fixture.CreateDefaultClient();
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/admin/products/201")
        {
            Content = JsonContent.Create(new ProductUpdateRequest(
                Name: "Field Tote 18L", Price: 95m, Stock: 40, CategoryId: 2, BrandId: 1, Description: "Updated")),
        };
        request.Headers.Add(AdminAutoAuthorizeMiddleware.AdminHeader, "true");

        var response = await httpClient.SendAsync(request, TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<AdminProduct>(TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal("Field Tote 18L", updated.Name);
        Assert.Equal(95m, updated.Price);

        // The dashboard wrote the change back to Catalog and recorded an audit row in admindb.
        Assert.Equal(95m, _fixture.ProductCatalog.Current(201).Price);
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
        var audit = db.ProductAudits.Single(a => a.ProductId == 201);
        Assert.Equal(89m, audit.OldPrice);
        Assert.Equal(95m, audit.NewPrice);
        Assert.Contains("Price", audit.Changes);
    }

    [Fact]
    public async Task UpdateReturnsNotFoundForMissingProduct()
    {
        var httpClient = _fixture.CreateDefaultClient();
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/admin/products/999999")
        {
            Content = JsonContent.Create(new ProductUpdateRequest(
                Name: "Ghost", Price: 1m, Stock: 1, CategoryId: 1, BrandId: 1, Description: null)),
        };
        request.Headers.Add(AdminAutoAuthorizeMiddleware.AdminHeader, "true");

        var response = await httpClient.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
