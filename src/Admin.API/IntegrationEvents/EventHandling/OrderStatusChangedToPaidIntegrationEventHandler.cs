using eShop.Admin.API.IntegrationEvents.Events;
using eShop.Admin.API.Services;

namespace eShop.Admin.API.IntegrationEvents.EventHandling;

/// <summary>Values a paid order via Catalog enrichment and folds it into the analytics facts.</summary>
public sealed class OrderStatusChangedToPaidIntegrationEventHandler(
    ICatalogEnricher enricher,
    IAnalyticsRecorder recorder,
    ILogger<OrderStatusChangedToPaidIntegrationEventHandler> logger)
    : IIntegrationEventHandler<OrderStatusChangedToPaidIntegrationEvent>
{
    public async Task Handle(OrderStatusChangedToPaidIntegrationEvent @event)
    {
        logger.LogInformation("Recording paid order {OrderId} into analytics", @event.OrderId);

        var items = @event.OrderStockItems?.ToList() ?? [];
        if (items.Count == 0)
        {
            return;
        }

        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var products = await enricher.GetProductsAsync(productIds, CancellationToken.None);

        var lines = items.Select(item =>
        {
            products.TryGetValue(item.ProductId, out var info);
            var name = info?.Name ?? $"Product {item.ProductId}";
            var category = info?.Category ?? "Uncategorized";
            var price = info?.Price ?? 0m;
            return new SaleLine(item.ProductId, name, category, item.Units, price * item.Units);
        }).ToList();

        var date = DateOnly.FromDateTime(@event.CreationDate);
        await recorder.RecordPaidOrderAsync(@event.OrderId, date, lines, @event.Id, CancellationToken.None);
    }
}
