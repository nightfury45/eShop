using eShop.Admin.API.Infrastructure;
using eShop.Admin.API.IntegrationEvents.Events;
using eShop.Admin.API.Services;
using eShop.EventBus.Abstractions;
using eShop.EventBus.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace eShop.Admin.UnitTests;

[TestClass]
public class ProductCatalogServiceTests
{
    private static AdminDbContext NewDbContext() =>
        new(new DbContextOptionsBuilder<AdminDbContext>()
            .UseInMemoryDatabase($"admindb-{Guid.NewGuid()}")
            .Options);

    private static CatalogItemDetail Item(int id, string name, decimal price, int stock, int type = 1, int brand = 1, int restock = 10) =>
        new(id, name, "desc", price, type, brand, stock, restock, 100, null);

    private static IProductCatalogClient ClientWith(CatalogItemDetail[] items)
    {
        var client = Substitute.For<IProductCatalogClient>();
        client.GetItemsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(new CatalogItemsPage(items, items.Length));
        foreach (var item in items)
        {
            client.GetItemAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        }
        client.GetCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<CatalogRef>>([new(1, "Footwear"), new(2, "Apparel")]);
        client.GetBrandsAsync(Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<CatalogRef>>([new(1, "Acme"), new(2, "Globex")]);
        return client;
    }

    private static ProductCatalogService NewService(IProductCatalogClient client, AdminDbContext db, IEventBus bus) =>
        new(client, db, bus, NullLogger<ProductCatalogService>.Instance);

    [TestMethod]
    public async Task ListAsync_maps_catalog_items_to_admin_products()
    {
        await using var db = NewDbContext();
        var client = ClientWith([Item(7, "Aero Runner", 129m, 412, type: 1, brand: 1)]);
        var service = NewService(client, db, Substitute.For<IEventBus>());

        var result = await service.ListAsync(new ProductQuery(0, 10, null, null, null), default);

        var product = result.Items.Single();
        Assert.AreEqual("Footwear", product.Category);
        Assert.AreEqual("Acme", product.Brand);
        Assert.AreEqual("SKU-0007", product.Sku);
        Assert.AreEqual("Active", product.Status);
        Assert.HasCount(2, result.Categories);
    }

    [TestMethod]
    public async Task ListAsync_derives_status_from_stock_levels()
    {
        await using var db = NewDbContext();
        var client = ClientWith([
            Item(1, "Full", 10m, 50, restock: 10),
            Item(2, "Low", 10m, 5, restock: 10),
            Item(3, "Out", 10m, 0, restock: 10),
        ]);
        var service = NewService(client, db, Substitute.For<IEventBus>());

        var result = await service.ListAsync(new ProductQuery(0, 10, null, null, null), default);

        Assert.AreEqual("Active", result.Items.Single(p => p.Id == 1).Status);
        Assert.AreEqual("LowStock", result.Items.Single(p => p.Id == 2).Status);
        Assert.AreEqual("OutOfStock", result.Items.Single(p => p.Id == 3).Status);
    }

    [TestMethod]
    public async Task UpdateAsync_writes_audit_and_publishes_event()
    {
        await using var db = NewDbContext();
        var client = ClientWith([Item(5, "Field Tote", 89m, 38, type: 1, brand: 1)]);
        var bus = Substitute.For<IEventBus>();
        var service = NewService(client, db, bus);

        var result = await service.UpdateAsync(
            5, new ProductUpdateRequest("Field Tote 18L", 95m, 40, CategoryId: 2, BrandId: 1, Description: "new"), "priya", default);

        Assert.IsNotNull(result);
        Assert.AreEqual("Field Tote 18L", result.Name);
        Assert.AreEqual("Apparel", result.Category);

        await client.Received(1).UpdateItemAsync(Arg.Is<CatalogItemDetail>(i => i.Price == 95m && i.CatalogTypeId == 2), Arg.Any<CancellationToken>());

        var audit = db.ProductAudits.Single();
        Assert.AreEqual(5, audit.ProductId);
        Assert.AreEqual("priya", audit.Editor);
        Assert.AreEqual(89m, audit.OldPrice);
        Assert.AreEqual(95m, audit.NewPrice);
        Assert.Contains("Price", audit.Changes);
        Assert.Contains("Category", audit.Changes);

        await bus.Received(1).PublishAsync(Arg.Is<AdminProductUpdatedIntegrationEvent>(
            e => e.ProductId == 5 && e.OldPrice == 89m && e.NewPrice == 95m && e.Editor == "priya"));
    }

    [TestMethod]
    public async Task UpdateAsync_returns_null_and_skips_side_effects_when_product_missing()
    {
        await using var db = NewDbContext();
        var client = Substitute.For<IProductCatalogClient>();
        client.GetItemAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(default(CatalogItemDetail));
        var bus = Substitute.For<IEventBus>();
        var service = NewService(client, db, bus);

        var result = await service.UpdateAsync(
            404, new ProductUpdateRequest("Ghost", 1m, 1, 1, 1, null), "priya", default);

        Assert.IsNull(result);
        Assert.AreEqual(0, db.ProductAudits.Count());
        await bus.DidNotReceive().PublishAsync(Arg.Any<IntegrationEvent>());
    }

    [TestMethod]
    public async Task UpdateAsync_swallows_publish_failures_after_persisting_audit()
    {
        await using var db = NewDbContext();
        var client = ClientWith([Item(9, "Trail Jacket", 219m, 12)]);
        var bus = Substitute.For<IEventBus>();
        bus.PublishAsync(Arg.Any<IntegrationEvent>()).Returns(Task.FromException(new InvalidOperationException("broker down")));
        var service = NewService(client, db, bus);

        var result = await service.UpdateAsync(
            9, new ProductUpdateRequest("Trail Jacket", 199m, 12, 1, 1, "x"), "priya", default);

        Assert.IsNotNull(result);
        Assert.AreEqual(1, db.ProductAudits.Count());
    }
}
