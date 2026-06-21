namespace eShop.Admin.API.IntegrationEvents.Events;

// Published by Admin.API when an administrator adjusts a product's on-hand stock from the Inventory
// screen. An admin-action (outbound) event, delivered through the transactional outbox so the event is
// persisted atomically with the dashboard's audit row and survives a broker outage. Distinct from
// Catalog's order-driven stock changes — this captures a deliberate manual correction.
public record AdminInventoryStockUpdatedIntegrationEvent(
    int ProductId,
    string ProductName,
    int OldOnHand,
    int NewOnHand,
    string Editor,
    string Reason) : IntegrationEvent;
