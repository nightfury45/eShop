using eShop.IntegrationEventLogEF.Services;
using eShop.IntegrationEventLogEF.Utilities;

namespace eShop.Admin.API.IntegrationEvents;

/// <summary>
/// Transactional-outbox publisher for the Admin Dashboard's outbound integration events. Mirrors eShop's
/// <c>CatalogIntegrationEventService</c>: the event is persisted to the <c>IntegrationEventLog</c> in
/// admindb atomically with the dashboard's own changes (audit rows), then published to the broker. A
/// broker outage cannot lose the event — it stays in the log for a later retry — and cannot roll back the
/// administrator's action.
/// </summary>
public interface IAdminIntegrationEventService
{
    /// <summary>
    /// Saves the currently-tracked <see cref="AdminDbContext"/> changes (e.g. an audit row) together with
    /// <paramref name="evt"/> in a single local transaction, so the audit and the outbox event commit or
    /// roll back as one.
    /// </summary>
    Task SaveEventAndAdminContextChangesAsync(IntegrationEvent evt);

    /// <summary>Publishes a previously-saved event through the event bus, updating its outbox state.</summary>
    Task PublishThroughEventBusAsync(IntegrationEvent evt);
}

public sealed class AdminIntegrationEventService(
    ILogger<AdminIntegrationEventService> logger,
    IEventBus eventBus,
    AdminDbContext adminContext,
    IIntegrationEventLogService eventLogService) : IAdminIntegrationEventService, IDisposable
{
    private volatile bool _disposedValue;

    public async Task PublishThroughEventBusAsync(IntegrationEvent evt)
    {
        try
        {
            logger.LogInformation("Publishing integration event: {IntegrationEventId} - ({@IntegrationEvent})", evt.Id, evt);

            await eventLogService.MarkEventAsInProgressAsync(evt.Id);
            await eventBus.PublishAsync(evt);
            await eventLogService.MarkEventAsPublishedAsync(evt.Id);
        }
        catch (Exception ex)
        {
            // The durable record is the saved outbox row; a broker outage must not fail the admin's action.
            logger.LogError(ex, "Error publishing integration event: {IntegrationEventId}", evt.Id);
            await eventLogService.MarkEventAsFailedAsync(evt.Id);
        }
    }

    public async Task SaveEventAndAdminContextChangesAsync(IntegrationEvent evt)
    {
        logger.LogInformation("AdminIntegrationEventService - Saving changes and integrationEvent: {IntegrationEventId}", evt.Id);

        // EF Core resiliency strategy when spanning the admin changes and the IntegrationEventLog within
        // one explicit transaction. See https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency
        await ResilientTransaction.New(adminContext).ExecuteAsync(async () =>
        {
            await adminContext.SaveChangesAsync();
            await eventLogService.SaveEventAsync(evt, adminContext.Database.CurrentTransaction);
        });
    }

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                (eventLogService as IDisposable)?.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
