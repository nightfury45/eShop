using System.Net.Http.Json;

namespace eShop.Admin.API.Services;

/// <summary>A catalog reference (id + display name) — used for category and brand lookups/filters.</summary>
public record CatalogRef(int Id, string Name);

/// <summary>The full catalog item shape the dashboard reads and writes back to Catalog.API.</summary>
public record CatalogItemDetail(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    int CatalogTypeId,
    int CatalogBrandId,
    int AvailableStock,
    int RestockThreshold,
    int MaxStockThreshold,
    string? PictureFileName,
    // Round-tripped so a dashboard write (product edit or stock adjustment) preserves Catalog's reorder
    // flag instead of clearing it — Catalog's PUT does a full SetValues replace, so any field the
    // dashboard omits is reset to its default.
    bool OnReorder);

/// <summary>A page of catalog items plus the total count, mirroring Catalog.API's PaginatedItems.</summary>
public record CatalogItemsPage(IReadOnlyList<CatalogItemDetail> Items, long TotalItems);

/// <summary>
/// Talks to Catalog.API over HTTP for the product-management screen: paged reads, single-item detail,
/// write-back, and the brand/type reference lists. Abstracted as an interface so the BFF aggregation
/// (and its tests) don't depend on a live Catalog service.
/// </summary>
public interface IProductCatalogClient
{
    Task<CatalogItemsPage> GetItemsAsync(
        int pageIndex, int pageSize, string? name, int? type, int? brand, CancellationToken cancellationToken);

    /// <summary>
    /// Reads the whole catalog (paging through Catalog.API up to a sane cap) so the Inventory screen can
    /// compute store-wide KPIs and filter/paginate consistently in one place. Suited to the sample's
    /// catalog size; a larger catalog would push this aggregation into the data store.
    /// </summary>
    Task<IReadOnlyList<CatalogItemDetail>> GetAllItemsAsync(CancellationToken cancellationToken);

    Task<CatalogItemDetail?> GetItemAsync(int id, CancellationToken cancellationToken);

    Task UpdateItemAsync(CatalogItemDetail item, CancellationToken cancellationToken);

    Task<IReadOnlyList<CatalogRef>> GetCategoriesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<CatalogRef>> GetBrandsAsync(CancellationToken cancellationToken);
}

public sealed class ProductCatalogClient(HttpClient httpClient) : IProductCatalogClient
{
    // Page size used when sweeping the whole catalog for the Inventory KPIs (Catalog clamps to 100).
    private const int AllItemsPageSize = 100;

    // Hard cap on how many products the inventory sweep will load, to bound a runaway loop.
    private const int AllItemsMax = 5000;

    // Catalog.API serialization shapes (camelCase via web defaults).
    private sealed record CatalogItemDto(
        int Id, string Name, string? Description, decimal Price,
        int CatalogTypeId, int CatalogBrandId, int AvailableStock,
        int RestockThreshold, int MaxStockThreshold, string? PictureFileName, bool OnReorder);

    private sealed record PaginatedItemsDto(int PageIndex, int PageSize, long Count, List<CatalogItemDto> Data);

    private sealed record CatalogTypeDto(int Id, string Type);
    private sealed record CatalogBrandDto(int Id, string Brand);

    public async Task<CatalogItemsPage> GetItemsAsync(
        int pageIndex, int pageSize, string? name, int? type, int? brand, CancellationToken cancellationToken)
    {
        var query = new List<string>
        {
            "api-version=2.0",
            $"PageIndex={pageIndex}",
            $"PageSize={pageSize}",
        };
        if (!string.IsNullOrWhiteSpace(name))
        {
            query.Add($"name={Uri.EscapeDataString(name)}");
        }
        if (type is not null)
        {
            query.Add($"type={type}");
        }
        if (brand is not null)
        {
            query.Add($"brand={brand}");
        }

        var page = await httpClient.GetFromJsonAsync<PaginatedItemsDto>(
            $"/api/catalog/items?{string.Join('&', query)}", cancellationToken);

        var items = page?.Data.Select(ToDetail).ToList() ?? [];
        return new CatalogItemsPage(items, page?.Count ?? 0);
    }

    public async Task<IReadOnlyList<CatalogItemDetail>> GetAllItemsAsync(CancellationToken cancellationToken)
    {
        var all = new List<CatalogItemDetail>();
        for (var pageIndex = 0; all.Count < AllItemsMax; pageIndex++)
        {
            var page = await httpClient.GetFromJsonAsync<PaginatedItemsDto>(
                $"/api/catalog/items?api-version=2.0&PageIndex={pageIndex}&PageSize={AllItemsPageSize}", cancellationToken);

            var batch = page?.Data;
            if (batch is null || batch.Count == 0)
            {
                break;
            }

            all.AddRange(batch.Select(ToDetail));

            if (batch.Count < AllItemsPageSize)
            {
                break;
            }
        }

        return all;
    }

    public async Task<CatalogItemDetail?> GetItemAsync(int id, CancellationToken cancellationToken)
    {
        var item = await httpClient.GetFromJsonAsync<CatalogItemDto>(
            $"/api/catalog/items/{id}?api-version=2.0", cancellationToken);
        return item is null ? null : ToDetail(item);
    }

    public async Task UpdateItemAsync(CatalogItemDetail item, CancellationToken cancellationToken)
    {
        // Catalog v2 PUT replaces the whole item and (server-side) re-emits ProductPriceChangedIntegrationEvent
        // when the price changes. We send the merged item back.
        var response = await httpClient.PutAsJsonAsync(
            $"/api/catalog/items/{item.Id}?api-version=2.0", item, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<CatalogRef>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        var types = await httpClient.GetFromJsonAsync<List<CatalogTypeDto>>(
            "/api/catalog/catalogtypes?api-version=1.0", cancellationToken) ?? [];
        return types.Select(t => new CatalogRef(t.Id, t.Type)).ToList();
    }

    public async Task<IReadOnlyList<CatalogRef>> GetBrandsAsync(CancellationToken cancellationToken)
    {
        var brands = await httpClient.GetFromJsonAsync<List<CatalogBrandDto>>(
            "/api/catalog/catalogbrands?api-version=1.0", cancellationToken) ?? [];
        return brands.Select(b => new CatalogRef(b.Id, b.Brand)).ToList();
    }

    private static CatalogItemDetail ToDetail(CatalogItemDto dto) => new(
        dto.Id, dto.Name, dto.Description, dto.Price, dto.CatalogTypeId, dto.CatalogBrandId,
        dto.AvailableStock, dto.RestockThreshold, dto.MaxStockThreshold, dto.PictureFileName, dto.OnReorder);
}
