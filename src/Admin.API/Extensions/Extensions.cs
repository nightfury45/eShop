public static class Extensions
{
    /// <summary>
    /// Registers Admin Dashboard BFF application services. In Epic 0 this wires the dashboard-owned
    /// OLTP database (admindb); later epics add cross-service HttpClients, the event bus, and the
    /// analytics (OLAP) context.
    /// </summary>
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<AdminDbContext>("admindb");
    }
}
