using Maintenance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Maintenance.Infrastructure.Persistence;

public sealed class MaintenanceDbContext : DbContext
{
    public MaintenanceDbContext(DbContextOptions<MaintenanceDbContext> options) : base(options) { }

    public DbSet<MaintenanceIssue> MaintenanceIssues => Set<MaintenanceIssue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("maintenance");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MaintenanceDbContext).Assembly);
    }
}
