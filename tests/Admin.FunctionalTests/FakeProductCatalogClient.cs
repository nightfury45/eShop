using eShop.Admin.API.Services;

namespace eShop.Admin.FunctionalTests;

/// <summary>In-memory <see cref="IProductCatalogClient"/> used to exercise the BFF product endpoints.</summary>
public sealed class FakeProductCatalogClient : IProductCatalogClient
{
    private readonly Dictionary<int, CatalogItemDetail> _items = new();

    public List<CatalogRef> Categories { get; } = [new(1, "Footwear"), new(2, "Apparel")];
    public List<CatalogRef> Brands { get; } = [new(1, "Acme"), new(2, "Globex")];

    public void Seed(params CatalogItemDetail[] items)
    {
        foreach (var item in items)
        {
            _items[item.Id] = item;
        }
    }

    public CatalogItemDetail Current(int id) => _items[id];

    public Task<IReadOnlyList<CatalogItemDetail>> GetAllItemsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<CatalogItemDetail>>(_items.Values.OrderBy(i => i.Name).ToList());

    public Task<CatalogItemsPage> GetItemsAsync(
        int pageIndex, int pageSize, string name, int? type, int? brand, CancellationToken cancellationToken)
    {
        IEnumerable<CatalogItemDetail> query = _items.Values.OrderBy(i => i.Name);
        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(i => i.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase));
        }
        if (type is not null)
        {
            query = query.Where(i => i.CatalogTypeId == type);
        }
        if (brand is not null)
        {
            query = query.Where(i => i.CatalogBrandId == brand);
        }

        var all = query.ToList();
        var paged = all.Skip(pageIndex * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new CatalogItemsPage(paged, all.Count));
    }

    public Task<CatalogItemDetail> GetItemAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(_items.TryGetValue(id, out var item) ? item : null);

    public Task UpdateItemAsync(CatalogItemDetail item, CancellationToken cancellationToken)
    {
        _items[item.Id] = item;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CatalogRef>> GetCategoriesAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<CatalogRef>>(Categories);

    public Task<IReadOnlyList<CatalogRef>> GetBrandsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<CatalogRef>>(Brands);
}
