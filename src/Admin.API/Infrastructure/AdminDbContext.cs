using eShop.Admin.API.Infrastructure.Inventory;
using eShop.Admin.API.Infrastructure.Products;
using eShop.IntegrationEventLogEF;

namespace eShop.Admin.API.Infrastructure;

/// <summary>
/// EF Core context for dashboard-owned OLTP data (audit logs, the integration-event outbox, and future
/// saved views / settings). Separate database from the OLAP analytics store; schema is created via
/// <see cref="AdminDbInitializer"/>.
/// </summary>
public class AdminDbContext(DbContextOptions<AdminDbContext> options) : DbContext(options)
{
    public DbSet<ProductAuditEntry> ProductAudits => Set<ProductAuditEntry>();

    public DbSet<InventoryAuditEntry> InventoryAudits => Set<InventoryAuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductAuditEntry>(entity =>
        {
            entity.ToTable("ProductAudits");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.Editor).HasMaxLength(200);
            entity.Property(e => e.Changes).HasMaxLength(500);
            entity.Property(e => e.OldPrice).HasPrecision(18, 2);
            entity.Property(e => e.NewPrice).HasPrecision(18, 2);
            entity.HasIndex(e => e.ProductId);
        });

        modelBuilder.Entity<InventoryAuditEntry>(entity =>
        {
            entity.ToTable("InventoryAudits");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.Editor).HasMaxLength(200);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.HasIndex(e => e.ProductId);
        });

        // Dashboard-owned transactional outbox: stock-adjustment events are persisted in admindb in the
        // same transaction as the audit row, then published by AdminIntegrationEventService.
        modelBuilder.UseIntegrationEventLogs();
    }
}
