using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomService.Domain.Entities;
using RoomService.Domain.Enums;

namespace RoomService.Infrastructure.Persistence.Configurations;

internal sealed class ServiceOrderConfiguration : IEntityTypeConfiguration<ServiceOrder>
{
    public void Configure(EntityTypeBuilder<ServiceOrder> builder)
    {
        builder.ToTable("ServiceOrders");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.BookingId).IsRequired();
        builder.Property(o => o.RoomNumber).HasMaxLength(10).IsRequired();
        builder.Property(o => o.Status).HasConversion<int>().IsRequired();
        builder.Property(o => o.TotalPrice).HasColumnType("numeric(10,2)").IsRequired();
        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.UpdatedAt).IsRequired();

        // OwnsMany: OrderLineItem is part of the ServiceOrder aggregate boundary.
        // Each line item row is owned exclusively by its parent order.
        builder.OwnsMany(o => o.LineItems, li =>
        {
            li.ToTable("ServiceOrderLineItems");
            li.WithOwner().HasForeignKey("ServiceOrderId");
            li.Property<int>("Id").ValueGeneratedOnAdd();
            li.HasKey("Id");

            li.Property(l => l.ItemName).HasMaxLength(100).IsRequired();
            li.Property(l => l.Quantity).IsRequired();
            li.Property(l => l.UnitPrice).HasColumnType("numeric(10,2)").IsRequired();
        });

        builder.HasIndex(o => o.BookingId).HasDatabaseName("IX_ServiceOrders_BookingId");
        builder.HasIndex(o => o.Status).HasDatabaseName("IX_ServiceOrders_Status");
    }
}
