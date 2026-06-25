using HotelOS.SharedKernel.Common;
using MediatR;
using RoomService.Application.DTOs;

namespace RoomService.Application.Commands.PlaceOrder;

public sealed record PlaceOrderCommand(
    Guid BookingId,
    string RoomNumber,
    IReadOnlyList<OrderLineItemRequest> Items) : IRequest<Result<ServiceOrderDto>>;

public sealed record OrderLineItemRequest(
    string ItemName,
    int Quantity,
    decimal UnitPrice);
