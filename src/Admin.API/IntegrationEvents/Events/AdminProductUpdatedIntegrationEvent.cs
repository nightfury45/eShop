namespace eShop.Admin.API.IntegrationEvents.Events;

// Published by Admin.API when an administrator edits a product from the dashboard. This is an
// admin-action (outbound) event — distinct from Catalog's ProductPriceChangedIntegrationEvent, which
// still fires from Catalog.API when the price changes. Downstream services can audit or react to
// dashboard-initiated edits without coupling to the dashboard.
public record AdminProductUpdatedIntegrationEvent(
    int ProductId,
    string ProductName,
    string Editor,
    decimal OldPrice,
    decimal NewPrice) : IntegrationEvent;
