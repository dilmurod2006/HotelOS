using Housekeeping.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Housekeeping.Infrastructure.Persistence;

public sealed class HousekeepingDbContext : DbContext
{
    public HousekeepingDbContext(DbContextOptions<HousekeepingDbContext> options) : base(options) { }

    public DbSet<CleaningTask> CleaningTasks => Set<CleaningTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("housekeeping");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HousekeepingDbContext).Assembly);
    }
}
