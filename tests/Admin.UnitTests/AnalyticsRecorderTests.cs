using eShop.Admin.API.Infrastructure;
using eShop.Admin.API.Infrastructure.Analytics;
using eShop.Admin.API.Services;
using Microsoft.EntityFrameworkCore;

namespace eShop.Admin.UnitTests;

[TestClass]
public class AnalyticsRecorderTests
{
    private static AnalyticsDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase($"analytics-{Guid.NewGuid()}")
            .Options);

    private static readonly DateOnly Date = new(2026, 6, 1);

    private static SaleLine[] TwoLines() =>
    [
        new SaleLine(10, "Aero Runner", "Footwear", 2, 40m),
        new SaleLine(11, "Field Tote", "Accessories", 1, 30m),
    ];

    [TestMethod]
    public async Task RecordPaidOrder_aggregates_daily_category_and_product_facts()
    {
        await using var ctx = NewContext();
        var recorder = new AnalyticsRecorder(ctx);

        await recorder.RecordPaidOrderAsync(1, Date, TwoLines(), Guid.NewGuid(), default);

        var daily = await ctx.DailySales.FindAsync(Date);
        Assert.IsNotNull(daily);
        Assert.AreEqual(1, daily.Orders);
        Assert.AreEqual(3, daily.Units);
        Assert.AreEqual(70m, daily.Revenue);

        Assert.AreEqual(40m, (await ctx.CategoryDaily.FindAsync(Date, "Footwear"))!.Revenue);
        Assert.AreEqual(2, (await ctx.ProductDaily.FindAsync(Date, 10))!.Units);
    }

    [TestMethod]
    public async Task RecordPaidOrder_is_idempotent_for_the_same_event_id()
    {
        await using var ctx = NewContext();
        var recorder = new AnalyticsRecorder(ctx);
        var eventId = Guid.NewGuid();

        await recorder.RecordPaidOrderAsync(1, Date, TwoLines(), eventId, default);
        await recorder.RecordPaidOrderAsync(1, Date, TwoLines(), eventId, default);

        var daily = await ctx.DailySales.FindAsync(Date);
        Assert.IsNotNull(daily);
        Assert.AreEqual(1, daily.Orders);
        Assert.AreEqual(70m, daily.Revenue);
    }

    [TestMethod]
    public async Task ReverseOrder_subtracts_the_recorded_contribution()
    {
        await using var ctx = NewContext();
        var recorder = new AnalyticsRecorder(ctx);
        await recorder.RecordPaidOrderAsync(1, Date, TwoLines(), Guid.NewGuid(), default);

        await recorder.ReverseOrderAsync(1, Guid.NewGuid(), default);

        var daily = await ctx.DailySales.FindAsync(Date);
        Assert.IsNotNull(daily);
        Assert.AreEqual(0, daily.Orders);
        Assert.AreEqual(0m, daily.Revenue);
        Assert.AreEqual(OrderSalesState.Cancelled, (await ctx.OrderSales.FindAsync(1))!.State);
    }

    [TestMethod]
    public async Task ReverseOrder_is_idempotent_for_the_same_event_id()
    {
        await using var ctx = NewContext();
        var recorder = new AnalyticsRecorder(ctx);
        await recorder.RecordPaidOrderAsync(1, Date, TwoLines(), Guid.NewGuid(), default);
        var cancelEvent = Guid.NewGuid();

        await recorder.ReverseOrderAsync(1, cancelEvent, default);
        await recorder.ReverseOrderAsync(1, cancelEvent, default);

        var daily = await ctx.DailySales.FindAsync(Date);
        Assert.IsNotNull(daily);
        Assert.AreEqual(0m, daily.Revenue);
    }
}
