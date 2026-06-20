using eShop.Admin.API.Infrastructure.Products;

namespace eShop.Admin.API.Infrastructure;

/// <summary>
/// EF Core context for dashboard-owned OLTP data (audit log, and future saved views / settings).
/// Separate database from the OLAP analytics store; schema is created via <see cref="AdminDbInitializer"/>.
/// </summary>
public class AdminDbContext(DbContextOptions<AdminDbContext> options) : DbContext(options)
{
    public DbSet<ProductAuditEntry> ProductAudits => Set<ProductAuditEntry>();

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
    }
}
