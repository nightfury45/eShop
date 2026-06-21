using eShop.Admin.API.Infrastructure.Analytics;

namespace eShop.Admin.API.Infrastructure;

/// <summary>
/// EF Core context for the read-optimized OLAP store (adminanalyticsdb). Holds denormalized,
/// pre-aggregated facts populated by integration-event consumers — never by replicating other
/// services' databases.
/// </summary>
public class AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : DbContext(options)
{
    public DbSet<DailySalesFact> DailySales => Set<DailySalesFact>();
    public DbSet<CategoryDailyFact> CategoryDaily => Set<CategoryDailyFact>();
    public DbSet<ProductDailyFact> ProductDaily => Set<ProductDailyFact>();
    public DbSet<OrderSalesRecord> OrderSales => Set<OrderSalesRecord>();
    public DbSet<OrderSalesLine> OrderSalesLines => Set<OrderSalesLine>();
    public DbSet<ProductInfo> Products => Set<ProductInfo>();
    public DbSet<ProcessedIntegrationEvent> ProcessedEvents => Set<ProcessedIntegrationEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<DailySalesFact>(e =>
        {
            e.HasKey(x => x.Date);
            e.Property(x => x.Revenue).HasPrecision(18, 2);
        });

        builder.Entity<CategoryDailyFact>(e =>
        {
            e.HasKey(x => new { x.Date, x.Category });
            e.Property(x => x.Category).HasMaxLength(128);
            e.Property(x => x.Revenue).HasPrecision(18, 2);
        });

        builder.Entity<ProductDailyFact>(e =>
        {
            e.HasKey(x => new { x.Date, x.ProductId });
            e.Property(x => x.ProductName).HasMaxLength(256);
            e.Property(x => x.Category).HasMaxLength(128);
            e.Property(x => x.Revenue).HasPrecision(18, 2);
        });

        builder.Entity<OrderSalesRecord>(e =>
        {
            e.HasKey(x => x.OrderId);
            e.Property(x => x.OrderId).ValueGeneratedNever();
            e.Property(x => x.Revenue).HasPrecision(18, 2);
            e.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OrderSalesLine>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductName).HasMaxLength(256);
            e.Property(x => x.Category).HasMaxLength(128);
            e.Property(x => x.Revenue).HasPrecision(18, 2);
        });

        builder.Entity<ProductInfo>(e =>
        {
            e.HasKey(x => x.ProductId);
            e.Property(x => x.ProductId).ValueGeneratedNever();
            e.Property(x => x.Name).HasMaxLength(256);
            e.Property(x => x.Category).HasMaxLength(128);
            e.Property(x => x.CurrentPrice).HasPrecision(18, 2);
        });

        builder.Entity<ProcessedIntegrationEvent>(e =>
        {
            e.HasKey(x => x.EventId);
            e.Property(x => x.EventType).HasMaxLength(256);
        });
    }
}
