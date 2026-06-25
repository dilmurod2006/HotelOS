namespace RoomService.Application.DTOs;

public sealed record ServiceOrderDto(
    Guid Id,
    Guid BookingId,
    string RoomNumber,
    string Status,
    int StatusStep,          // 1=Received, 2=Preparing, 3=Delivering, 4=Delivered
    decimal TotalPrice,
    IReadOnlyList<OrderLineItemDto> LineItems,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record OrderLineItemDto(
    string ItemName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);
