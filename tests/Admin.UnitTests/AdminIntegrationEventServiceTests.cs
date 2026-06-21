using eShop.Admin.API.Infrastructure;
using eShop.Admin.API.IntegrationEvents;
using eShop.Admin.API.IntegrationEvents.Events;
using eShop.EventBus.Abstractions;
using eShop.EventBus.Events;
using eShop.IntegrationEventLogEF.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace eShop.Admin.UnitTests;

[TestClass]
public class AdminIntegrationEventServiceTests
{
    private static AdminDbContext NewDbContext() =>
        new(new DbContextOptionsBuilder<AdminDbContext>()
            .UseInMemoryDatabase($"admindb-{Guid.NewGuid()}")
            .Options);

    private static AdminInventoryStockUpdatedIntegrationEvent SampleEvent() =>
        new(ProductId: 1, ProductName: "Aero Runner", OldOnHand: 10, NewOnHand: 20, Editor: "priya", Reason: "count");

    [TestMethod]
    public async Task PublishThroughEventBus_marks_published_on_success()
    {
        await using var db = NewDbContext();
        var bus = Substitute.For<IEventBus>();
        var log = Substitute.For<IIntegrationEventLogService>();
        var service = new AdminIntegrationEventService(NullLogger<AdminIntegrationEventService>.Instance, bus, db, log);
        var evt = SampleEvent();

        await service.PublishThroughEventBusAsync(evt);

        await log.Received(1).MarkEventAsInProgressAsync(evt.Id);
        await bus.Received(1).PublishAsync(evt);
        await log.Received(1).MarkEventAsPublishedAsync(evt.Id);
        await log.DidNotReceive().MarkEventAsFailedAsync(evt.Id);
    }

    [TestMethod]
    public async Task PublishThroughEventBus_swallows_broker_failure_and_marks_failed()
    {
        await using var db = NewDbContext();
        var bus = Substitute.For<IEventBus>();
        bus.PublishAsync(Arg.Any<IntegrationEvent>()).Returns(Task.FromException(new InvalidOperationException("broker down")));
        var log = Substitute.For<IIntegrationEventLogService>();
        var service = new AdminIntegrationEventService(NullLogger<AdminIntegrationEventService>.Instance, bus, db, log);
        var evt = SampleEvent();

        // Must not throw — the saved outbox row is the durable record.
        await service.PublishThroughEventBusAsync(evt);

        await log.Received(1).MarkEventAsFailedAsync(evt.Id);
        await log.DidNotReceive().MarkEventAsPublishedAsync(evt.Id);
    }
}
