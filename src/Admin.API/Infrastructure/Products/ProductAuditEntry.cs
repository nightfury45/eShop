namespace eShop.Admin.API.Infrastructure.Products;

/// <summary>
/// Dashboard-owned audit record (OLTP, admindb) capturing an administrator edit to a catalog product.
/// The Catalog service remains the system of record for the product itself; this is the dashboard's
/// durable trail of who changed what, written in the same request that calls Catalog.API.
/// </summary>
public class ProductAuditEntry
{
    public Guid Id { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    /// <summary>The administrator (subject/name claim) who performed the edit.</summary>
    public string Editor { get; set; } = string.Empty;

    public decimal OldPrice { get; set; }

    public decimal NewPrice { get; set; }

    public int OldStock { get; set; }

    public int NewStock { get; set; }

    /// <summary>Comma-separated list of the fields that changed (e.g. "Price, Stock").</summary>
    public string Changes { get; set; } = string.Empty;

    public DateTime TimestampUtc { get; set; }
}
