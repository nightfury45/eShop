using eShop.Admin.API.IntegrationEvents.Events;
using eShop.Admin.API.Services;

namespace eShop.Admin.API.IntegrationEvents.EventHandling;

/// <summary>Keeps the dashboard's cached product price fresh.</summary>
public sealed class ProductPriceChangedIntegrationEventHandler(
    IAnalyticsRecorder recorder,
    ILogger<ProductPriceChangedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<ProductPriceChangedIntegrationEvent>
{
    public async Task Handle(ProductPriceChangedIntegrationEvent @event)
    {
        logger.LogInformation("Updating cached price for product {ProductId}", @event.ProductId);
        await recorder.UpdateProductPriceAsync(@event.ProductId, @event.NewPrice, @event.Id, CancellationToken.None);
    }
}
