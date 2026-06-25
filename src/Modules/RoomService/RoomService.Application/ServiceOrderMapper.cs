using RoomService.Application.DTOs;
using RoomService.Domain.Entities;

namespace RoomService.Application;

internal static class ServiceOrderMapper
{
    internal static ServiceOrderDto ToDto(ServiceOrder order)
        => new(
            Id:         order.Id,
            BookingId:  order.BookingId,
            RoomNumber: order.RoomNumber,
            Status:     order.Status.ToString(),
            StatusStep: (int)order.Status,
            TotalPrice: order.TotalPrice,
            LineItems:  order.LineItems
                            .Select(li => new OrderLineItemDto(
                                li.ItemName, li.Quantity, li.UnitPrice,
                                li.UnitPrice * li.Quantity))
                            .ToList()
                            .AsReadOnly(),
            CreatedAt:  order.CreatedAt,
            UpdatedAt:  order.UpdatedAt);
}
