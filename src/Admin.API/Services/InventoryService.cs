using eShop.Admin.API.Infrastructure;
using eShop.Admin.API.Infrastructure.Inventory;
using eShop.Admin.API.IntegrationEvents;
using eShop.Admin.API.IntegrationEvents.Events;

namespace eShop.Admin.API.Services;

/// <summary>A single inventory row: catalog stock plus dashboard-derived SKU and stock status.</summary>
public record InventoryItem(
    int Id,
    string Name,
    string Sku,
    int OnHand,
    int ReorderThreshold,
    string Status,
    decimal Price);

/// <summary>Store-wide inventory KPIs shown above the table.</summary>
public record InventorySummary(int TotalSkus, int LowStockCount, int OutOfStockCount, decimal InventoryValue);

/// <summary>A page of inventory rows plus the store-wide KPI summary (computed over the whole catalog).</summary>
public record InventoryResult(
    IReadOnlyList<InventoryItem> Items,
    int PageIndex,
    int PageSize,
    long TotalItems,
    InventorySummary Summary);

public record InventoryQuery(int PageIndex, int PageSize, string? Search, bool LowStockOnly);

/// <summary>The fields the dashboard sends to adjust a product's on-hand stock.</summary>
public record StockAdjustment(int NewOnHand, string Reason);

/// <summary>
/// BFF aggregation for the Inventory screen. Reads the catalog via <see cref="IProductCatalogClient"/>,
/// derives stock status + KPIs, and on a stock adjustment writes the new on-hand back to Catalog, records
/// a durable audit row, and publishes <see cref="AdminInventoryStockUpdatedIntegrationEvent"/> through the
/// transactional outbox.
/// </summary>
public interface IInventoryService
{
    Task<InventoryResult> GetInventoryAsync(InventoryQuery query, CancellationToken cancellationToken);

    Task<InventoryItem?> AdjustAsync(int id, StockAdjustment adjustment, string editor, CancellationToken cancellationToken);
}

public sealed class InventoryService(
    IProductCatalogClient catalog,
    AdminDbContext dbContext,
    IAdminIntegrationEventService integrationEvents) : IInventoryService
{
    public async Task<InventoryResult> GetInventoryAsync(InventoryQuery query, CancellationToken cancellationToken)
    {
        // Fetch the whole catalog once so KPIs, filtering and pagination are all computed from the same
        // snapshot (avoids the page-local filter vs total mismatch). Suited to the sample catalog size.
        var all = (await catalog.GetAllItemsAsync(cancellationToken)).Select(Map).ToList();

        var summary = new InventorySummary(
            TotalSkus: all.Count,
            LowStockCount: all.Count(i => i.Status == "LowStock"),
            OutOfStockCount: all.Count(i => i.Status == "OutOfStock"),
            InventoryValue: all.Sum(i => i.Price * i.OnHand));

        IEnumerable<InventoryItem> filtered = all;
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            filtered = filtered.Where(i =>
                i.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                i.Sku.Contains(term, StringComparison.OrdinalIgnoreCase));
        }
        if (query.LowStockOnly)
        {
            // "Below reorder point" — low or out of stock.
            filtered = filtered.Where(i => i.Status is "LowStock" or "OutOfStock");
        }

        var ordered = filtered.OrderBy(i => i.Name).ToList();
        var page = ordered.Skip(query.PageIndex * query.PageSize).Take(query.PageSize).ToList();

        return new InventoryResult(page, query.PageIndex, query.PageSize, ordered.Count, summary);
    }

    public async Task<InventoryItem?> AdjustAsync(
        int id, StockAdjustment adjustment, string editor, CancellationToken cancellationToken)
    {
        // Re-read immediately before the write to narrow the lost-update window against order-driven
        // stock changes (full optimistic concurrency is tracked as a hardening follow-up).
        var existing = await catalog.GetItemAsync(id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var updated = existing with
        {
            AvailableStock = adjustment.NewOnHand,
            // Keep Catalog's reorder flag consistent with the new level (and preserved across the full PUT).
            OnReorder = adjustment.NewOnHand <= existing.RestockThreshold,
        };

        await catalog.UpdateItemAsync(updated, cancellationToken);

        dbContext.InventoryAudits.Add(new InventoryAuditEntry
        {
            Id = Guid.NewGuid(),
            ProductId = id,
            ProductName = existing.Name,
            Editor = editor,
            OldOnHand = existing.AvailableStock,
            NewOnHand = adjustment.NewOnHand,
            Reason = adjustment.Reason,
            TimestampUtc = DateTime.UtcNow,
        });

        var integrationEvent = new AdminInventoryStockUpdatedIntegrationEvent(
            id, existing.Name, existing.AvailableStock, adjustment.NewOnHand, editor, adjustment.Reason);
        await integrationEvents.SaveEventAndAdminContextChangesAsync(integrationEvent);
        await integrationEvents.PublishThroughEventBusAsync(integrationEvent);

        return Map(updated);
    }

    private static InventoryItem Map(CatalogItemDetail item) => new(
        Id: item.Id,
        Name: item.Name,
        Sku: ProductCatalogService.DeriveSku(item.Id),
        OnHand: item.AvailableStock,
        ReorderThreshold: item.RestockThreshold,
        Status: ProductCatalogService.DeriveStatus(item.AvailableStock, item.RestockThreshold),
        Price: item.Price);
}
