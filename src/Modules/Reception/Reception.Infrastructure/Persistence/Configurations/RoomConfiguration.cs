using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reception.Domain.Entities;

namespace Reception.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core entity configuration for the Room aggregate root.
///
/// ══ Value Object Mapping ══
/// RoomNumber and Money are mapped via OwnsOne (owned entity types).
/// OwnsOne flattens the value object's properties into the owner's table —
/// no JOIN is needed to hydrate the full aggregate.
///
/// ══ Constructor Binding ══
/// EF Core 8 discovers the private constructor private RoomNumber(int floor, int unit)
/// via reflection and calls it with values from the DB columns. The constructor then
/// computes RoomNumber.Value = "{floor}{unit:D2}" — so Value is always correct in
/// memory without being persisted as a separate column.
///
/// ══ Optimistic Concurrency ══
/// IsConcurrencyToken() on RowVersion tells EF Core to include the original
/// byte[] value in every UPDATE's WHERE clause:
///   UPDATE "Rooms" SET ... WHERE "Id" = @id AND "RowVersion" = @originalVersion
/// If 0 rows are affected (another process changed the row), EF Core throws
/// DbUpdateConcurrencyException → UnitOfWork re-throws as ConcurrencyConflictException.
/// The UoW regenerates the RowVersion bytes before every Add/Modify save so
/// each write is distinguishable from the previous one.
/// </summary>
internal sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever(); // Guid set by domain factory

        // ── RoomNumber (Value Object → inline columns) ────────────────────────
        // EF Core calls: private RoomNumber(int floor, int unit)
        // Parameter names 'floor'/'unit' match properties Floor/Unit (case-insensitive).
        // Value is computed by the constructor from Floor+Unit — ignored here to avoid
        // a mapping conflict. Repositories query by Floor+Unit, not the string Value.
        builder.OwnsOne(r => r.RoomNumber, rn =>
        {
            rn.Property(n => n.Floor)
                .HasColumnName("RoomNumber_Floor")
                .IsRequired();

            rn.Property(n => n.Unit)
                .HasColumnName("RoomNumber_Unit")
                .IsRequired();

            rn.Ignore(n => n.Value);

            // No two rooms can occupy the same floor+unit slot
            rn.HasIndex(new[] { "Floor", "Unit" })
                .IsUnique()
                .HasDatabaseName("UQ_Rooms_RoomNumber");
        });

        // ── NightlyRate (Money Value Object → inline columns) ─────────────────
        // Uses decimal(18,2) — exact financial arithmetic, never float/double.
        builder.OwnsOne(r => r.NightlyRate, m =>
        {
            m.Property(mo => mo.Amount)
                .HasColumnName("NightlyRate_Amount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            m.Property(mo => mo.Currency)
                .HasColumnName("NightlyRate_Currency")
                .HasMaxLength(3)
                .IsFixedLength() // ISO 4217: always exactly 3 chars (USD, GBP, EUR…)
                .IsRequired();
        });

        builder.Property(r => r.Type)
            .HasConversion<string>() // Stored as "Standard", "Deluxe" etc. — readable in DB
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Floor).IsRequired();
        builder.Property(r => r.IsNearElevator).IsRequired();
        builder.Property(r => r.IsNearStaircase).IsRequired();
        builder.Property(r => r.LastCleanedAt); // Nullable — null = never cleaned (new room)
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt).IsRequired();

        // ── Optimistic Concurrency Token ──────────────────────────────────────
        // IsConcurrencyToken (not IsRowVersion) because PostgreSQL lacks a native
        // rowversion type. The UnitOfWork generates Guid.NewGuid().ToByteArray() on
        // every Add/Modify. EF Core tracks the original bytes and includes them in
        // the UPDATE WHERE clause — mismatch → DbUpdateConcurrencyException.
        builder.Property(r => r.RowVersion)
            .HasColumnName("RowVersion")
            .HasColumnType("bytea")
            .IsConcurrencyToken()
            .IsRequired();

        // ── Performance Indexes ───────────────────────────────────────────────

        // Room assignment algorithm: WHERE Type = ? AND Status IN ('Available','Clean')
        builder.HasIndex(r => new { r.Type, r.Status })
            .HasDatabaseName("IX_Rooms_Type_Status");

        // ORDER BY LastCleanedAt ASC — "longest clean" sort criterion
        builder.HasIndex(r => r.LastCleanedAt)
            .HasDatabaseName("IX_Rooms_LastCleanedAt");
    }
}
