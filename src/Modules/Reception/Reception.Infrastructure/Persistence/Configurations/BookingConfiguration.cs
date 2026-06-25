using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reception.Domain.Entities;
using Reception.Domain.Enums;

namespace Reception.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core entity configuration for the Booking aggregate root.
///
/// ══ Multiple Money Columns ══
/// Booking has four Money value objects (RoomCharge, RoomServiceCharges, ExtraFees,
/// TotalBill). Each is flattened with a distinct column prefix to avoid collision:
///   RoomCharge_Amount, RoomCharge_Currency
///   RoomServiceCharges_Amount, RoomServiceCharges_Currency
///   ExtraFees_Amount, ExtraFees_Currency
///   TotalBill_Amount, TotalBill_Currency  ← nullable (null until checkout)
///
/// ══ ExtraFeeLineItems (OwnsMany) ══
/// Stored in a separate table "BookingExtraFees" with a shadow integer PK.
/// EF Core writes to the private _extraFeeLineItems backing field directly
/// (configured in ReceptionDbContext.OnModelCreating).
///
/// ══ DateRange.NightCount ══
/// Computed property (CheckOut.DayNumber - CheckIn.DayNumber) — ignored in EF Core.
/// The correct value is recomputed in memory after materialization.
/// </summary>
internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.GuestId).IsRequired();
        builder.Property(b => b.RoomId).IsRequired();

        // ── StayPeriod (DateRange Value Object) ───────────────────────────────
        // NightCount is a computed C# property — ignored in EF Core, recomputed on read.
        // DateOnly maps natively to PostgreSQL "date" type via Npgsql.
        builder.OwnsOne(b => b.StayPeriod, sp =>
        {
            sp.Property(d => d.CheckIn)
                .HasColumnName("CheckIn")
                .IsRequired();

            sp.Property(d => d.CheckOut)
                .HasColumnName("CheckOut")
                .IsRequired();

            sp.Ignore(d => d.NightCount);
        });

        // ── Money: RoomCharge ─────────────────────────────────────────────────
        builder.OwnsOne(b => b.RoomCharge, m =>
        {
            m.Property(mo => mo.Amount)
                .HasColumnName("RoomCharge_Amount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();
            m.Property(mo => mo.Currency)
                .HasColumnName("RoomCharge_Currency")
                .HasMaxLength(3).IsFixedLength().IsRequired();
        });

        // ── Money: RoomServiceCharges ─────────────────────────────────────────
        builder.OwnsOne(b => b.RoomServiceCharges, m =>
        {
            m.Property(mo => mo.Amount)
                .HasColumnName("RoomServiceCharges_Amount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();
            m.Property(mo => mo.Currency)
                .HasColumnName("RoomServiceCharges_Currency")
                .HasMaxLength(3).IsFixedLength().IsRequired();
        });

        // ── Money: ExtraFees ──────────────────────────────────────────────────
        builder.OwnsOne(b => b.ExtraFees, m =>
        {
            m.Property(mo => mo.Amount)
                .HasColumnName("ExtraFees_Amount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();
            m.Property(mo => mo.Currency)
                .HasColumnName("ExtraFees_Currency")
                .HasMaxLength(3).IsFixedLength().IsRequired();
        });

        // ── Money: TotalBill (nullable — null until CheckOut seals the bill) ──
        // EF Core treats all TotalBill columns as nullable. When all are null,
        // the owned entity is null — matches our domain model (TotalBill = null pre-checkout).
        builder.OwnsOne(b => b.TotalBill, m =>
        {
            m.Property(mo => mo.Amount)
                .HasColumnName("TotalBill_Amount")
                .HasColumnType("decimal(18,2)");
            m.Property(mo => mo.Currency)
                .HasColumnName("TotalBill_Currency")
                .HasMaxLength(3).IsFixedLength();
        });

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(b => b.RequestedRoomType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(b => b.PreferredFloor); // Nullable int

        builder.Property(b => b.ProximityPreference)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.UpdatedAt).IsRequired();

        // ── ExtraFeeLineItems (OwnsMany → separate table) ─────────────────────
        // Each booking accumulates extra charges (minibar, late checkout, etc.).
        // Shadow integer PK "Id" is auto-generated by EF Core.
        // ExtraFeeLineItem.Amount (Money) is a nested OwnsOne within the owned collection.
        builder.OwnsMany(b => b.ExtraFeeLineItems, li =>
        {
            li.ToTable("BookingExtraFees");
            li.WithOwner().HasForeignKey("BookingId");
            li.Property<int>("Id").ValueGeneratedOnAdd();
            li.HasKey("Id");

            li.Property(i => i.Description)
                .HasMaxLength(200)
                .IsRequired();

            li.Property(i => i.AddedAt).IsRequired();

            li.OwnsOne(i => i.Amount, m =>
            {
                m.Property(mo => mo.Amount)
                    .HasColumnName("Amount_Value")
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();
                m.Property(mo => mo.Currency)
                    .HasColumnName("Amount_Currency")
                    .HasMaxLength(3).IsFixedLength().IsRequired();
            });
        });

        // ── Foreign Keys ──────────────────────────────────────────────────────
        // Restrict deletes — hotel data must be retained for audit/compliance.
        builder.HasOne(b => b.Guest)
            .WithMany(g => g.Bookings)
            .HasForeignKey(b => b.GuestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Room)
            .WithMany(r => r.Bookings)
            .HasForeignKey(b => b.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Performance Indexes ───────────────────────────────────────────────

        // CheckOutCommandHandler: find the active booking for a specific room
        builder.HasIndex(b => new { b.RoomId, b.Status })
            .HasDatabaseName("IX_Bookings_RoomId_Status");

        // Guest booking history queries
        builder.HasIndex(b => b.GuestId)
            .HasDatabaseName("IX_Bookings_GuestId");
    }
}
