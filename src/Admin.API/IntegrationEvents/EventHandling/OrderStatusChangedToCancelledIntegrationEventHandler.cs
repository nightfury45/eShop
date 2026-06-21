using eShop.Admin.API.IntegrationEvents.Events;
using eShop.Admin.API.Services;

namespace eShop.Admin.API.IntegrationEvents.EventHandling;

/// <summary>Reverses a previously recorded sale when its order is cancelled.</summary>
public sealed class OrderStatusChangedToCancelledIntegrationEventHandler(
    IAnalyticsRecorder recorder,
    ILogger<OrderStatusChangedToCancelledIntegrationEventHandler> logger)
    : IIntegrationEventHandler<OrderStatusChangedToCancelledIntegrationEvent>
{
    public async Task Handle(OrderStatusChangedToCancelledIntegrationEvent @event)
    {
        logger.LogInformation("Reversing cancelled order {OrderId} in analytics", @event.OrderId);
        await recorder.ReverseOrderAsync(@event.OrderId, @event.Id, CancellationToken.None);
    }
}
