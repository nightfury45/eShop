using eShop.Admin.API.Infrastructure;
using eShop.Admin.API.Infrastructure.Analytics;

namespace eShop.Admin.API.Services;

/// <summary>A valued line of a paid order, ready to fold into the analytics facts.</summary>
public record SaleLine(int ProductId, string ProductName, string Category, int Units, decimal Revenue);

/// <summary>
/// Applies integration events to the OLAP facts. All operations are idempotent (guarded by the
/// processed-event ledger) so at-least-once delivery cannot double-count.
/// </summary>
public interface IAnalyticsRecorder
{
    Task RecordPaidOrderAsync(int orderId, DateOnly date, IReadOnlyCollection<SaleLine> lines, Guid eventId, CancellationToken cancellationToken);
    Task ReverseOrderAsync(int orderId, Guid eventId, CancellationToken cancellationToken);
    Task UpdateProductPriceAsync(int productId, decimal newPrice, Guid eventId, CancellationToken cancellationToken);
}

public sealed class AnalyticsRecorder(AnalyticsDbContext db) : IAnalyticsRecorder
{
    public async Task RecordPaidOrderAsync(
        int orderId, DateOnly date, IReadOnlyCollection<SaleLine> lines, Guid eventId, CancellationToken cancellationToken)
    {
        if (await AlreadyProcessedAsync(eventId, cancellationToken))
        {
            return;
        }

        // Order-level guard so a re-published paid event (new event id) still cannot double-count.
        if (!await db.OrderSales.AnyAsync(o => o.OrderId == orderId, cancellationToken))
        {
            var orderUnits = lines.Sum(l => l.Units);
            var orderRevenue = lines.Sum(l => l.Revenue);

            db.OrderSales.Add(new OrderSalesRecord
            {
                OrderId = orderId,
                Date = date,
                Revenue = orderRevenue,
                Units = orderUnits,
                State = OrderSalesState.Recorded,
                Lines = lines.Select(l => new OrderSalesLine
                {
                    OrderId = orderId,
                    ProductId = l.ProductId,
                    ProductName = l.ProductName,
                    Category = l.Category,
                    Units = l.Units,
                    Revenue = l.Revenue,
                }).ToList(),
            });

            var daily = await GetOrCreateDailyAsync(date, cancellationToken);
            daily.Revenue += orderRevenue;
            daily.Units += orderUnits;
            daily.Orders += 1;

            foreach (var line in lines)
            {
                var category = await GetOrCreateCategoryAsync(date, line.Category, cancellationToken);
                category.Revenue += line.Revenue;
                category.Units += line.Units;

                var product = await GetOrCreateProductAsync(date, line.ProductId, cancellationToken);
                product.ProductName = line.ProductName;
                product.Category = line.Category;
                product.Revenue += line.Revenue;
                product.Units += line.Units;

                await UpsertProductInfoAsync(line.ProductId, line.ProductName, line.Category, line.Revenue / Math.Max(line.Units, 1), cancellationToken);
            }
        }

        MarkProcessed(eventId, nameof(RecordPaidOrderAsync));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReverseOrderAsync(int orderId, Guid eventId, CancellationToken cancellationToken)
    {
        if (await AlreadyProcessedAsync(eventId, cancellationToken))
        {
            return;
        }

        var order = await db.OrderSales.Include(o => o.Lines).FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);
        if (order is { State: OrderSalesState.Recorded })
        {
            var daily = await db.DailySales.FindAsync([order.Date], cancellationToken);
            if (daily is not null)
            {
                daily.Revenue -= order.Revenue;
                daily.Units -= order.Units;
                daily.Orders -= 1;
            }

            foreach (var line in order.Lines)
            {
                var category = await db.CategoryDaily.FindAsync([order.Date, line.Category], cancellationToken);
                if (category is not null)
                {
                    category.Revenue -= line.Revenue;
                    category.Units -= line.Units;
                }

                var product = await db.ProductDaily.FindAsync([order.Date, line.ProductId], cancellationToken);
                if (product is not null)
                {
                    product.Revenue -= line.Revenue;
                    product.Units -= line.Units;
                }
            }

            order.State = OrderSalesState.Cancelled;
        }

        MarkProcessed(eventId, nameof(ReverseOrderAsync));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateProductPriceAsync(int productId, decimal newPrice, Guid eventId, CancellationToken cancellationToken)
    {
        if (await AlreadyProcessedAsync(eventId, cancellationToken))
        {
            return;
        }

        var info = await db.Products.FindAsync([productId], cancellationToken);
        if (info is null)
        {
            db.Products.Add(new ProductInfo { ProductId = productId, CurrentPrice = newPrice, UpdatedAt = DateTime.UtcNow });
        }
        else
        {
            info.CurrentPrice = newPrice;
            info.UpdatedAt = DateTime.UtcNow;
        }

        MarkProcessed(eventId, nameof(UpdateProductPriceAsync));
        await db.SaveChangesAsync(cancellationToken);
    }

    private Task<bool> AlreadyProcessedAsync(Guid eventId, CancellationToken cancellationToken) =>
        db.ProcessedEvents.AnyAsync(e => e.EventId == eventId, cancellationToken);

    private void MarkProcessed(Guid eventId, string eventType) =>
        db.ProcessedEvents.Add(new ProcessedIntegrationEvent { EventId = eventId, EventType = eventType, ProcessedAt = DateTime.UtcNow });

    private async Task<DailySalesFact> GetOrCreateDailyAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var fact = await db.DailySales.FindAsync([date], cancellationToken);
        if (fact is null)
        {
            fact = new DailySalesFact { Date = date };
            db.DailySales.Add(fact);
        }
        return fact;
    }

    private async Task<CategoryDailyFact> GetOrCreateCategoryAsync(DateOnly date, string category, CancellationToken cancellationToken)
    {
        var fact = await db.CategoryDaily.FindAsync([date, category], cancellationToken);
        if (fact is null)
        {
            fact = new CategoryDailyFact { Date = date, Category = category };
            db.CategoryDaily.Add(fact);
        }
        return fact;
    }

    private async Task<ProductDailyFact> GetOrCreateProductAsync(DateOnly date, int productId, CancellationToken cancellationToken)
    {
        var fact = await db.ProductDaily.FindAsync([date, productId], cancellationToken);
        if (fact is null)
        {
            fact = new ProductDailyFact { Date = date, ProductId = productId };
            db.ProductDaily.Add(fact);
        }
        return fact;
    }

    private async Task UpsertProductInfoAsync(int productId, string name, string category, decimal price, CancellationToken cancellationToken)
    {
        var info = await db.Products.FindAsync([productId], cancellationToken);
        if (info is null)
        {
            db.Products.Add(new ProductInfo { ProductId = productId, Name = name, Category = category, CurrentPrice = price, UpdatedAt = DateTime.UtcNow });
        }
        else
        {
            info.Name = name;
            info.Category = category;
            info.UpdatedAt = DateTime.UtcNow;
        }
    }
}
