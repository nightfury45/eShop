namespace eShop.Admin.API.Infrastructure;

/// <summary>
/// EF Core context for dashboard-owned OLTP data (saved views, settings, audit log).
/// Empty in Epic 0 — entities and migrations are introduced by later epics (Products, Settings).
/// </summary>
public class AdminDbContext(DbContextOptions<AdminDbContext> options) : DbContext(options)
{
}
