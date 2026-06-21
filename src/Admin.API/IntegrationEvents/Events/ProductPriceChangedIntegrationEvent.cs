namespace eShop.Admin.API.IntegrationEvents.Events;

// Published by Catalog.API when a product price changes; refreshes the dashboard's product cache.
public record ProductPriceChangedIntegrationEvent(int ProductId, decimal NewPrice, decimal OldPrice) : IntegrationEvent;
