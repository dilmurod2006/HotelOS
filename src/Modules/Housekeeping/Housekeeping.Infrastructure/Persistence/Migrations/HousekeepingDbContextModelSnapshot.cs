using System;
using Housekeeping.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Housekeeping.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(HousekeepingDbContext))]
    partial class HousekeepingDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("housekeeping")
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Housekeeping.Domain.Entities.CleaningTask", b =>
            {
                b.Property<Guid>("Id").HasColumnType("uuid");
                b.Property<Guid?>("AssignedCleanerId").HasColumnType("uuid");
                b.Property<DateTime?>("CompletedAt").HasColumnType("timestamp with time zone");
                b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
                b.Property<int>("Floor").HasColumnType("integer");
                b.Property<int>("Priority").HasColumnType("integer");
                b.Property<Guid>("RoomId").HasColumnType("uuid");
                b.Property<string>("RoomNumber").IsRequired().HasMaxLength(10).HasColumnType("character varying(10)");
                b.Property<DateTime?>("StartedAt").HasColumnType("timestamp with time zone");
                b.Property<int>("Status").HasColumnType("integer");

                b.HasKey("Id");
                b.HasIndex("RoomId").HasDatabaseName("IX_CleaningTasks_RoomId");
                b.HasIndex("Status").HasDatabaseName("IX_CleaningTasks_Status");
                b.ToTable("CleaningTasks", "housekeeping");
            });
#pragma warning restore 612, 618
        }
    }
}
