using System.Net.Http.Json;

namespace eShop.Admin.API.Services;

/// <summary>Product facts needed to value a sale: name, category and unit price.</summary>
public record CatalogProductInfo(int ProductId, string Name, string Category, decimal Price);

/// <summary>Resolves product metadata/prices from Catalog.API so the analytics consumer can value sales.</summary>
public interface ICatalogEnricher
{
    Task<IReadOnlyDictionary<int, CatalogProductInfo>> GetProductsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken);
}

public sealed class CatalogEnricher(HttpClient httpClient, ILogger<CatalogEnricher> logger) : ICatalogEnricher
{
    private sealed record CatalogItemDto(int Id, string Name, decimal Price, int CatalogTypeId);
    private sealed record CatalogTypeDto(int Id, string Type);

    public async Task<IReadOnlyDictionary<int, CatalogProductInfo>> GetProductsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<int, CatalogProductInfo>();
        if (productIds.Count == 0)
        {
            return result;
        }

        try
        {
            var idQuery = string.Join("&", productIds.Distinct().Select(id => $"ids={id}"));
            var items =
                await httpClient.GetFromJsonAsync<List<CatalogItemDto>>(
                    $"/api/catalog/items/by?api-version=1.0&{idQuery}", cancellationToken) ?? [];

            var types =
                await httpClient.GetFromJsonAsync<List<CatalogTypeDto>>(
                    "/api/catalog/catalogtypes?api-version=1.0", cancellationToken) ?? [];
            var typeNames = types.ToDictionary(t => t.Id, t => t.Type);

            foreach (var item in items)
            {
                var category = typeNames.TryGetValue(item.CatalogTypeId, out var name) ? name : "Uncategorized";
                result[item.Id] = new CatalogProductInfo(item.Id, item.Name, category, item.Price);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to enrich products from Catalog.API for ids {ProductIds}", productIds);
        }

        return result;
    }
}
