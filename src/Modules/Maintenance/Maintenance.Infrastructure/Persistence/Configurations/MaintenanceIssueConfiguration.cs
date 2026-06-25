using Maintenance.Domain.Entities;
using Maintenance.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maintenance.Infrastructure.Persistence.Configurations;

internal sealed class MaintenanceIssueConfiguration : IEntityTypeConfiguration<MaintenanceIssue>
{
    public void Configure(EntityTypeBuilder<MaintenanceIssue> builder)
    {
        builder.ToTable("MaintenanceIssues");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.RoomId).IsRequired();
        builder.Property(i => i.RoomNumber).HasMaxLength(10).IsRequired();
        builder.Property(i => i.Description).HasMaxLength(500).IsRequired();

        builder.Property(i => i.Urgency)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(i => i.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(i => i.AssignedTechnicianId);
        builder.Property(i => i.ResolutionNotes).HasMaxLength(1000);
        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.UpdatedAt).IsRequired();
        builder.Property(i => i.ResolvedAt);

        // Priority Queue index: the DB pre-sorts the queue at the storage level
        // ORDER BY Urgency ASC (Critical=1 first), CreatedAt ASC (FIFO within same urgency)
        builder.HasIndex(i => new { i.Urgency, i.CreatedAt })
            .HasDatabaseName("IX_MaintenanceIssues_PriorityQueue")
            .HasFilter($"\"Status\" != {(int)IssueStatus.Resolved}");

        builder.HasIndex(i => i.RoomId).HasDatabaseName("IX_MaintenanceIssues_RoomId");
    }
}
