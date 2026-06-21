namespace eShop.Admin.API.Infrastructure.Analytics;

/// <summary>Per-day store totals (drives the revenue series and headline KPIs).</summary>
public class DailySalesFact
{
    public DateOnly Date { get; set; }
    public decimal Revenue { get; set; }
    public int Orders { get; set; }
    public int Units { get; set; }
}

/// <summary>Per-day revenue/units by category (drives "Top categories" for a period).</summary>
public class CategoryDailyFact
{
    public DateOnly Date { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int Units { get; set; }
}

/// <summary>Per-day revenue/units by product (drives "Top selling products" for a period).</summary>
public class ProductDailyFact
{
    public DateOnly Date { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int Units { get; set; }
}

public enum OrderSalesState
{
    Recorded = 0,
    Cancelled = 1,
}

/// <summary>
/// The contribution a single paid order made to the facts, retained so a later cancellation can
/// reverse exactly what was recorded.
/// </summary>
public class OrderSalesRecord
{
    public int OrderId { get; set; }
    public DateOnly Date { get; set; }
    public decimal Revenue { get; set; }
    public int Units { get; set; }
    public OrderSalesState State { get; set; }
    public List<OrderSalesLine> Lines { get; set; } = [];
}

public class OrderSalesLine
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Units { get; set; }
    public decimal Revenue { get; set; }
}

/// <summary>Cached product metadata (kept fresh via enrichment + ProductPriceChanged events).</summary>
public class ProductInfo
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Idempotency ledger: integration events already applied to the facts.</summary>
public class ProcessedIntegrationEvent
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}
