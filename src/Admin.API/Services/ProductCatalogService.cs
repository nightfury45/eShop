using eShop.Admin.API.Infrastructure;
using eShop.Admin.API.Infrastructure.Products;
using eShop.Admin.API.IntegrationEvents.Events;

namespace eShop.Admin.API.Services;

/// <summary>A product as presented on the dashboard: catalog data plus dashboard-derived SKU and status.</summary>
public record AdminProduct(
    int Id,
    string Name,
    string Sku,
    int CategoryId,
    string Category,
    int BrandId,
    string Brand,
    decimal Price,
    int Stock,
    int RestockThreshold,
    string Status,
    string? Description);

/// <summary>A page of dashboard products plus the category/brand reference lists used by the filters.</summary>
public record AdminProductsResult(
    IReadOnlyList<AdminProduct> Items,
    int PageIndex,
    int PageSize,
    long TotalItems,
    IReadOnlyList<CatalogRef> Categories,
    IReadOnlyList<CatalogRef> Brands);

/// <summary>The editable fields the dashboard sends when an administrator saves the edit drawer.</summary>
public record ProductUpdateRequest(
    string Name,
    decimal Price,
    int Stock,
    int CategoryId,
    int BrandId,
    string? Description);

public record ProductQuery(int PageIndex, int PageSize, string? Name, int? CategoryId, int? BrandId);

/// <summary>
/// BFF aggregation for the product-management screen. Reads from Catalog.API (via
/// <see cref="IProductCatalogClient"/>), projects catalog items into the dashboard shape, and on update
/// writes a durable audit row to admindb and publishes an admin-action integration event.
/// </summary>
public interface IProductCatalogService
{
    Task<AdminProductsResult> ListAsync(ProductQuery query, CancellationToken cancellationToken);

    Task<AdminProduct?> GetAsync(int id, CancellationToken cancellationToken);

    Task<AdminProduct?> UpdateAsync(int id, ProductUpdateRequest request, string editor, CancellationToken cancellationToken);
}

public sealed class ProductCatalogService(
    IProductCatalogClient catalog,
    AdminDbContext dbContext,
    IEventBus eventBus,
    ILogger<ProductCatalogService> logger) : IProductCatalogService
{
    public async Task<AdminProductsResult> ListAsync(ProductQuery query, CancellationToken cancellationToken)
    {
        var page = await catalog.GetItemsAsync(
            query.PageIndex, query.PageSize, query.Name, query.CategoryId, query.BrandId, cancellationToken);
        var categories = await catalog.GetCategoriesAsync(cancellationToken);
        var brands = await catalog.GetBrandsAsync(cancellationToken);

        var categoryNames = categories.ToDictionary(c => c.Id, c => c.Name);
        var brandNames = brands.ToDictionary(b => b.Id, b => b.Name);

        var items = page.Items
            .Select(item => Map(item, categoryNames, brandNames))
            .ToList();

        return new AdminProductsResult(items, query.PageIndex, query.PageSize, page.TotalItems, categories, brands);
    }

    public async Task<AdminProduct?> GetAsync(int id, CancellationToken cancellationToken)
    {
        var item = await catalog.GetItemAsync(id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        var categoryNames = (await catalog.GetCategoriesAsync(cancellationToken)).ToDictionary(c => c.Id, c => c.Name);
        var brandNames = (await catalog.GetBrandsAsync(cancellationToken)).ToDictionary(b => b.Id, b => b.Name);
        return Map(item, categoryNames, brandNames);
    }

    public async Task<AdminProduct?> UpdateAsync(
        int id, ProductUpdateRequest request, string editor, CancellationToken cancellationToken)
    {
        var existing = await catalog.GetItemAsync(id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var updated = existing with
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            AvailableStock = request.Stock,
            CatalogTypeId = request.CategoryId,
            CatalogBrandId = request.BrandId,
        };

        await catalog.UpdateItemAsync(updated, cancellationToken);

        // Durable audit trail in admindb — the dashboard's record of who changed what.
        var changes = DescribeChanges(existing, updated);
        dbContext.ProductAudits.Add(new ProductAuditEntry
        {
            Id = Guid.NewGuid(),
            ProductId = id,
            ProductName = updated.Name,
            Editor = editor,
            OldPrice = existing.Price,
            NewPrice = updated.Price,
            OldStock = existing.AvailableStock,
            NewStock = updated.AvailableStock,
            Changes = changes,
            TimestampUtc = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        await PublishUpdatedAsync(id, existing, updated, editor);

        var categoryNames = (await catalog.GetCategoriesAsync(cancellationToken)).ToDictionary(c => c.Id, c => c.Name);
        var brandNames = (await catalog.GetBrandsAsync(cancellationToken)).ToDictionary(b => b.Id, b => b.Name);
        return Map(updated, categoryNames, brandNames);
    }

    private async Task PublishUpdatedAsync(int id, CatalogItemDetail before, CatalogItemDetail after, string editor)
    {
        // Best-effort: the admindb audit row above is the durable record. A broker outage must not fail
        // the administrator's save, so a publish failure is logged rather than propagated.
        try
        {
            await eventBus.PublishAsync(new AdminProductUpdatedIntegrationEvent(
                id, after.Name, editor, before.Price, after.Price));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish AdminProductUpdatedIntegrationEvent for product {ProductId}", id);
        }
    }

    private static string DescribeChanges(CatalogItemDetail before, CatalogItemDetail after)
    {
        var changes = new List<string>();
        if (before.Name != after.Name) changes.Add("Name");
        if (before.Price != after.Price) changes.Add("Price");
        if (before.AvailableStock != after.AvailableStock) changes.Add("Stock");
        if (before.CatalogTypeId != after.CatalogTypeId) changes.Add("Category");
        if (before.CatalogBrandId != after.CatalogBrandId) changes.Add("Brand");
        if (before.Description != after.Description) changes.Add("Description");
        return string.Join(", ", changes);
    }

    private static AdminProduct Map(
        CatalogItemDetail item,
        IReadOnlyDictionary<int, string> categoryNames,
        IReadOnlyDictionary<int, string> brandNames) => new(
            Id: item.Id,
            Name: item.Name,
            Sku: DeriveSku(item.Id),
            CategoryId: item.CatalogTypeId,
            Category: categoryNames.TryGetValue(item.CatalogTypeId, out var c) ? c : "Uncategorized",
            BrandId: item.CatalogBrandId,
            Brand: brandNames.TryGetValue(item.CatalogBrandId, out var b) ? b : "Unbranded",
            Price: item.Price,
            Stock: item.AvailableStock,
            RestockThreshold: item.RestockThreshold,
            Status: DeriveStatus(item.AvailableStock, item.RestockThreshold),
            Description: item.Description);

    /// <summary>Catalog has no SKU field, so the dashboard shows a stable derived SKU from the product id.</summary>
    public static string DeriveSku(int id) => $"SKU-{id:D4}";

    /// <summary>
    /// Catalog has no status flag, so status is derived from stock levels: out of stock at zero, low when
    /// at or below the restock threshold, otherwise active.
    /// </summary>
    public static string DeriveStatus(int stock, int restockThreshold) =>
        stock <= 0 ? "OutOfStock"
        : stock <= restockThreshold ? "LowStock"
        : "Active";
}
