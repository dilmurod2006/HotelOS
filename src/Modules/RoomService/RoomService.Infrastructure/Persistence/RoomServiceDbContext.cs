using Microsoft.EntityFrameworkCore;
using RoomService.Domain.Entities;

namespace RoomService.Infrastructure.Persistence;

public sealed class RoomServiceDbContext : DbContext
{
    public RoomServiceDbContext(DbContextOptions<RoomServiceDbContext> options) : base(options) { }

    public DbSet<ServiceOrder> ServiceOrders => Set<ServiceOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("roomservice");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RoomServiceDbContext).Assembly);

        // Private backing field for LineItems collection
        modelBuilder.Entity<ServiceOrder>()
            .Navigation(o => o.LineItems)
            .HasField("_lineItems")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
