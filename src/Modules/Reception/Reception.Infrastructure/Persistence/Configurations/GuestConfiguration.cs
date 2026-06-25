using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reception.Domain.Entities;

namespace Reception.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core entity configuration for the Guest aggregate root.
///
/// ══ Data Minimisation (ISO/IEC 27001) ══
/// Only the minimum fields required for hotel operations are persisted.
/// Full payment card numbers are NEVER stored — only MaskedCardLast4 (the last 4
/// digits, formatted as "****-****-****-1234") is kept for receipt display.
///
/// ══ GuestName OwnsOne ══
/// GuestName.FullName is a computed property ("FirstName LastName") — ignored in
/// EF Core, recomputed from the stored FirstName and LastName columns.
/// </summary>
internal sealed class GuestConfiguration : IEntityTypeConfiguration<Guest>
{
    public void Configure(EntityTypeBuilder<Guest> builder)
    {
        builder.ToTable("Guests");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();

        // ── GuestName (Value Object → inline columns) ─────────────────────────
        // FullName is computed — ignored so EF Core does not try to persist it.
        builder.OwnsOne(g => g.Name, n =>
        {
            n.Property(gn => gn.FirstName)
                .HasColumnName("FirstName")
                .HasMaxLength(50)
                .IsRequired();

            n.Property(gn => gn.LastName)
                .HasColumnName("LastName")
                .HasMaxLength(50)
                .IsRequired();

            n.Ignore(gn => gn.FullName);
        });

        builder.Property(g => g.Email)
            .HasMaxLength(254) // RFC 5321 maximum email length
            .IsRequired();

        // Unique email — used as a natural key for guest lookup
        builder.HasIndex(g => g.Email)
            .IsUnique()
            .HasDatabaseName("UQ_Guests_Email");

        builder.Property(g => g.PhoneNumber)
            .HasMaxLength(20)
            .IsRequired();

        // ISO/IEC 27001 data minimisation: "****-****-****-1234" = 19 chars max
        builder.Property(g => g.MaskedCardLast4)
            .HasMaxLength(20);

        builder.Property(g => g.CreatedAt).IsRequired();
    }
}
