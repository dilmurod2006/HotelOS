using Housekeeping.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Housekeeping.Infrastructure.Persistence.Configurations;

internal sealed class CleaningTaskConfiguration : IEntityTypeConfiguration<CleaningTask>
{
    public void Configure(EntityTypeBuilder<CleaningTask> builder)
    {
        builder.ToTable("CleaningTasks");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.RoomId).IsRequired();
        builder.Property(t => t.RoomNumber).HasMaxLength(10).IsRequired();
        builder.Property(t => t.Floor).IsRequired();
        builder.Property(t => t.Status).HasConversion<int>().IsRequired();
        builder.Property(t => t.Priority).HasConversion<int>().IsRequired();
        builder.Property(t => t.AssignedCleanerId);
        builder.Property(t => t.StartedAt);
        builder.Property(t => t.CompletedAt);
        builder.Property(t => t.CreatedAt).IsRequired();

        builder.HasIndex(t => t.RoomId).HasDatabaseName("IX_CleaningTasks_RoomId");
        builder.HasIndex(t => t.Status).HasDatabaseName("IX_CleaningTasks_Status");
    }
}
