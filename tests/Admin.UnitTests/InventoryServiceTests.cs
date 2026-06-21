using eShop.Admin.API.Infrastructure;
using eShop.Admin.API.IntegrationEvents;
using eShop.Admin.API.IntegrationEvents.Events;
using eShop.Admin.API.Services;
using eShop.EventBus.Events;
using Microsoft.EntityFrameworkCore;

namespace eShop.Admin.UnitTests;

[TestClass]
public class InventoryServiceTests
{
    private static AdminDbContext NewDbContext() =>
        new(new DbContextOptionsBuilder<AdminDbContext>()
            .UseInMemoryDatabase($"admindb-{Guid.NewGuid()}")
            .Options);

    private static CatalogItemDetail Item(int id, string name, decimal price, int stock, int restock = 10, bool onReorder = false) =>
        new(id, name, "desc", price, 1, 1, stock, restock, 100, null, onReorder);

    private static IProductCatalogClient ClientWith(params CatalogItemDetail[] items)
    {
        var client = Substitute.For<IProductCatalogClient>();
        client.GetAllItemsAsync(Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<CatalogItemDetail>>(items.ToList());
        foreach (var item in items)
        {
            client.GetItemAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        }
        return client;
    }

    // Outbox stand-in that persists the tracked audit row when the event is saved (mirrors the real one).
    private static IAdminIntegrationEventService EventsThatSave(AdminDbContext db)
    {
        var events = Substitute.For<IAdminIntegrationEventService>();
        events.SaveEventAndAdminContextChangesAsync(Arg.Any<IntegrationEvent>())
            .Returns(_ => db.SaveChangesAsync());
        return events;
    }

    [TestMethod]
    public async Task GetInventory_computes_store_wide_summary_and_status()
    {
        await using var db = NewDbContext();
        var client = ClientWith(
            Item(1, "Full", 10m, 50, restock: 10),
            Item(2, "Low", 10m, 5, restock: 10),
            Item(3, "Out", 10m, 0, restock: 10));
        var service = new InventoryService(client, db, EventsThatSave(db));

        var result = await service.GetInventoryAsync(new InventoryQuery(0, 10, null, false), default);

        Assert.AreEqual(3, result.Summary.TotalSkus);
        Assert.AreEqual(1, result.Summary.LowStockCount);
        Assert.AreEqual(1, result.Summary.OutOfStockCount);
        Assert.AreEqual(550m, result.Summary.InventoryValue); // 10*50 + 10*5 + 10*0
        Assert.AreEqual("SKU-0001", result.Items.Single(i => i.Id == 1).Sku);
        Assert.AreEqual("LowStock", result.Items.Single(i => i.Id == 2).Status);
        Assert.AreEqual("OutOfStock", result.Items.Single(i => i.Id == 3).Status);
    }

    [TestMethod]
    public async Task GetInventory_low_stock_only_filters_rows_but_keeps_full_summary()
    {
        await using var db = NewDbContext();
        var client = ClientWith(
            Item(1, "Full", 10m, 50, restock: 10),
            Item(2, "Low", 10m, 5, restock: 10),
            Item(3, "Out", 10m, 0, restock: 10));
        var service = new InventoryService(client, db, EventsThatSave(db));

        var result = await service.GetInventoryAsync(new InventoryQuery(0, 10, null, LowStockOnly: true), default);

        Assert.AreEqual(2, result.TotalItems); // low + out
        Assert.IsFalse(result.Items.Any(i => i.Status == "Active"));
        Assert.AreEqual(3, result.Summary.TotalSkus); // summary still spans the whole catalog
    }

    [TestMethod]
    public async Task GetInventory_search_matches_name_or_sku()
    {
        await using var db = NewDbContext();
        var client = ClientWith(
            Item(1, "Aero Runner", 10m, 50),
            Item(42, "Field Tote", 10m, 50));
        var service = new InventoryService(client, db, EventsThatSave(db));

        var byName = await service.GetInventoryAsync(new InventoryQuery(0, 10, "aero", false), default);
        Assert.AreEqual(1, byName.TotalItems);
        Assert.AreEqual("Aero Runner", byName.Items.Single().Name);

        var bySku = await service.GetInventoryAsync(new InventoryQuery(0, 10, "SKU-0042", false), default);
        Assert.AreEqual(1, bySku.TotalItems);
        Assert.AreEqual(42, bySku.Items.Single().Id);
    }

    [TestMethod]
    public async Task GetInventory_paginates_filtered_rows()
    {
        await using var db = NewDbContext();
        var items = Enumerable.Range(1, 25).Select(i => Item(i, $"Item {i:D2}", 5m, 100)).ToArray();
        var service = new InventoryService(ClientWith(items), db, EventsThatSave(db));

        var page2 = await service.GetInventoryAsync(new InventoryQuery(PageIndex: 1, PageSize: 10, null, false), default);

        Assert.AreEqual(25, page2.TotalItems);
        Assert.HasCount(10, page2.Items);
        Assert.AreEqual("Item 11", page2.Items.First().Name); // ordered by name, second page
    }

    [TestMethod]
    public async Task Adjust_updates_stock_recomputes_reorder_writes_audit_and_publishes()
    {
        await using var db = NewDbContext();
        var client = ClientWith(Item(5, "Field Tote", 89m, 38, restock: 40, onReorder: false));
        var events = EventsThatSave(db);
        var service = new InventoryService(client, db, events);

        var result = await service.AdjustAsync(5, new StockAdjustment(NewOnHand: 12, Reason: "Damaged units"), "priya", default);

        Assert.IsNotNull(result);
        Assert.AreEqual(12, result.OnHand);
        Assert.AreEqual("LowStock", result.Status); // 12 <= restock 40

        // New on-hand written back, and OnReorder recomputed to true (12 <= 40) and preserved through the PUT.
        await client.Received(1).UpdateItemAsync(
            Arg.Is<CatalogItemDetail>(i => i.AvailableStock == 12 && i.OnReorder), Arg.Any<CancellationToken>());

        var audit = db.InventoryAudits.Single();
        Assert.AreEqual(5, audit.ProductId);
        Assert.AreEqual("priya", audit.Editor);
        Assert.AreEqual(38, audit.OldOnHand);
        Assert.AreEqual(12, audit.NewOnHand);
        Assert.AreEqual("Damaged units", audit.Reason);

        await events.Received(1).SaveEventAndAdminContextChangesAsync(Arg.Is<AdminInventoryStockUpdatedIntegrationEvent>(
            e => e.ProductId == 5 && e.OldOnHand == 38 && e.NewOnHand == 12 && e.Editor == "priya"));
        await events.Received(1).PublishThroughEventBusAsync(Arg.Is<AdminInventoryStockUpdatedIntegrationEvent>(e => e.ProductId == 5));
    }

    [TestMethod]
    public async Task Adjust_above_threshold_clears_reorder_flag()
    {
        await using var db = NewDbContext();
        var client = ClientWith(Item(6, "Trail Jacket", 219m, 2, restock: 25, onReorder: true));
        var service = new InventoryService(client, db, EventsThatSave(db));

        await service.AdjustAsync(6, new StockAdjustment(NewOnHand: 100, Reason: "Restocked"), "priya", default);

        await client.Received(1).UpdateItemAsync(
            Arg.Is<CatalogItemDetail>(i => i.AvailableStock == 100 && !i.OnReorder), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Adjust_returns_null_and_skips_side_effects_when_product_missing()
    {
        await using var db = NewDbContext();
        var client = Substitute.For<IProductCatalogClient>();
        client.GetItemAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(default(CatalogItemDetail));
        var events = EventsThatSave(db);
        var service = new InventoryService(client, db, events);

        var result = await service.AdjustAsync(404, new StockAdjustment(10, "x"), "priya", default);

        Assert.IsNull(result);
        Assert.AreEqual(0, db.InventoryAudits.Count());
        await events.DidNotReceive().SaveEventAndAdminContextChangesAsync(Arg.Any<IntegrationEvent>());
        await client.DidNotReceive().UpdateItemAsync(Arg.Any<CatalogItemDetail>(), Arg.Any<CancellationToken>());
    }
}
