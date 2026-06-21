namespace eShop.Admin.API.Infrastructure.Inventory;

/// <summary>
/// Dashboard-owned audit record (OLTP, admindb) capturing an administrator stock adjustment on a catalog
/// product. Catalog remains the system of record for the stock value itself; this is the dashboard's
/// durable trail of who changed the on-hand quantity, by how much, and why — written atomically with the
/// outbound integration event.
/// </summary>
public class InventoryAuditEntry
{
    public Guid Id { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    /// <summary>The administrator (name/subject claim) who performed the adjustment.</summary>
    public string Editor { get; set; } = string.Empty;

    public int OldOnHand { get; set; }

    public int NewOnHand { get; set; }

    /// <summary>The reason the administrator gave for the adjustment (e.g. "Cycle count correction").</summary>
    public string Reason { get; set; } = string.Empty;

    public DateTime TimestampUtc { get; set; }
}
