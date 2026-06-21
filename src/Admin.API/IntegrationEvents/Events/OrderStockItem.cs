namespace eShop.Admin.API.IntegrationEvents.Events;

// Local copy of the eShop integration-event contract (matched by type name + shape on the wire).
public record OrderStockItem
{
    public int ProductId { get; }
    public int Units { get; }

    public OrderStockItem(int productId, int units)
    {
        ProductId = productId;
        Units = units;
    }
}
