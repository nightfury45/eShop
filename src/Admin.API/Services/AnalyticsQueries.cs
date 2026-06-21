using eShop.Admin.API.Infrastructure;

namespace eShop.Admin.API.Services;

public record KpiValue(decimal Value, decimal DeltaPercent, IReadOnlyList<decimal> Spark);

public record RevenuePoint(DateOnly Date, decimal Current, decimal Previous);

public record CategorySlice(string Category, decimal Revenue);

public record ProductRow(int ProductId, string Name, string Category, int Units, decimal Revenue, decimal SharePercent);

public record AnalyticsSummary(
    int PeriodDays,
    KpiValue Revenue,
    KpiValue Orders,
    KpiValue AverageOrderValue,
    KpiValue Units,
    IReadOnlyList<RevenuePoint> RevenueSeries,
    IReadOnlyList<CategorySlice> TopCategories,
    IReadOnlyList<ProductRow> TopProducts);

public interface IAnalyticsQueries
{
    Task<AnalyticsSummary> GetSummaryAsync(int days, DateOnly today, CancellationToken cancellationToken);
}

public sealed class AnalyticsQueries(AnalyticsDbContext db) : IAnalyticsQueries
{
    public async Task<AnalyticsSummary> GetSummaryAsync(int days, DateOnly today, CancellationToken cancellationToken)
    {
        var currentStart = today.AddDays(-(days - 1));
        var prevStart = today.AddDays(-(2 * days - 1));
        var prevEnd = today.AddDays(-days);

        var daily = await db.DailySales
            .Where(d => d.Date >= prevStart && d.Date <= today)
            .ToListAsync(cancellationToken);
        var dailyByDate = daily.ToDictionary(d => d.Date);

        bool InCurrent(DateOnly d) => d >= currentStart && d <= today;
        bool InPrevious(DateOnly d) => d >= prevStart && d <= prevEnd;

        var cur = daily.Where(d => InCurrent(d.Date)).ToList();
        var prev = daily.Where(d => InPrevious(d.Date)).ToList();

        var curRevenue = cur.Sum(d => d.Revenue);
        var curOrders = cur.Sum(d => d.Orders);
        var curUnits = cur.Sum(d => d.Units);
        var prevRevenue = prev.Sum(d => d.Revenue);
        var prevOrders = prev.Sum(d => d.Orders);
        var prevUnits = prev.Sum(d => d.Units);

        var curAov = curOrders > 0 ? curRevenue / curOrders : 0m;
        var prevAov = prevOrders > 0 ? prevRevenue / prevOrders : 0m;

        var series = new List<RevenuePoint>(days);
        var sparkRevenue = new List<decimal>(days);
        var sparkOrders = new List<decimal>(days);
        var sparkUnits = new List<decimal>(days);
        var sparkAov = new List<decimal>(days);
        for (var i = 0; i < days; i++)
        {
            var cd = currentStart.AddDays(i);
            var pd = prevStart.AddDays(i);
            dailyByDate.TryGetValue(cd, out var c);
            series.Add(new RevenuePoint(
                cd,
                c is not null ? Round2(c.Revenue) : 0m,
                dailyByDate.TryGetValue(pd, out var p) ? Round2(p.Revenue) : 0m));
            sparkRevenue.Add(c is not null ? Round2(c.Revenue) : 0m);
            sparkOrders.Add(c?.Orders ?? 0);
            sparkUnits.Add(c?.Units ?? 0);
            sparkAov.Add(c is { Orders: > 0 } ? Round2(c.Revenue / c.Orders) : 0m);
        }

        var categoryRows = await db.CategoryDaily
            .Where(c => c.Date >= currentStart && c.Date <= today)
            .ToListAsync(cancellationToken);
        var topCategories = categoryRows
            .GroupBy(c => c.Category)
            .Select(g => new CategorySlice(g.Key, Round2(g.Sum(x => x.Revenue))))
            .OrderByDescending(c => c.Revenue)
            .Take(5)
            .ToList();

        var productRows = await db.ProductDaily
            .Where(p => p.Date >= currentStart && p.Date <= today)
            .ToListAsync(cancellationToken);
        var totalProductRevenue = productRows.Sum(p => p.Revenue);
        var topProducts = productRows
            .GroupBy(p => p.ProductId)
            .Select(g =>
            {
                var revenue = g.Sum(x => x.Revenue);
                var latest = g.OrderByDescending(x => x.Date).First();
                var share = totalProductRevenue > 0 ? revenue / totalProductRevenue * 100m : 0m;
                return new ProductRow(g.Key, latest.ProductName, latest.Category, g.Sum(x => x.Units), Round2(revenue), Round1(share));
            })
            .OrderByDescending(p => p.Revenue)
            .Take(5)
            .ToList();

        return new AnalyticsSummary(
            days,
            new KpiValue(Round2(curRevenue), Delta(curRevenue, prevRevenue), sparkRevenue),
            new KpiValue(curOrders, Delta(curOrders, prevOrders), sparkOrders),
            new KpiValue(Round2(curAov), Delta(curAov, prevAov), sparkAov),
            new KpiValue(curUnits, Delta(curUnits, prevUnits), sparkUnits),
            series,
            topCategories,
            topProducts);
    }

    private static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static decimal Round1(decimal value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);

    private static decimal Delta(decimal current, decimal previous)
    {
        if (previous == 0m)
        {
            return current > 0m ? 100m : 0m;
        }
        return Round1((current - previous) / previous * 100m);
    }
}
