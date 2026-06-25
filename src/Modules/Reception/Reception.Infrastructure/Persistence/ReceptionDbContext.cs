using Microsoft.EntityFrameworkCore;
using Reception.Domain.Entities;

namespace Reception.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the Reception bounded context.
/// Scoped to the "reception" PostgreSQL schema to provide physical module isolation
/// even though all modules share the same database server.
///
/// All entity-to-table mappings are defined in IEntityTypeConfiguration classes
/// (Configurations/ folder) and applied via ApplyConfigurationsFromAssembly — the
/// DbContext itself stays free of mapping details (Single Responsibility Principle).
/// </summary>
public sealed class ReceptionDbContext : DbContext
{
    public ReceptionDbContext(DbContextOptions<ReceptionDbContext> options) : base(options) { }

    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Guest> Guests => Set<Guest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // All tables live in the "reception" schema — prevents table name collisions
        // with other modules (Housekeeping, Maintenance, RoomService) in the same DB.
        modelBuilder.HasDefaultSchema("reception");

        // Discovers and applies all IEntityTypeConfiguration<T> classes in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReceptionDbContext).Assembly);

        // ── Backing field wiring for read-only collection navigations ─────────────
        // Domain entities expose IReadOnlyCollection<T> to prevent external mutation.
        // EF Core must write directly to the private List<T> backing field to
        // populate these collections during materialization.
        modelBuilder.Entity<Booking>()
            .Navigation(b => b.ExtraFeeLineItems)
            .HasField("_extraFeeLineItems")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        modelBuilder.Entity<Room>()
            .Navigation(r => r.Bookings)
            .HasField("_bookings")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        modelBuilder.Entity<Guest>()
            .Navigation(g => g.Bookings)
            .HasField("_bookings")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
