using HotelOS.SharedKernel.Common;
using MediatR;
using RoomService.Application.DTOs;
using RoomService.Domain.Enums;

namespace RoomService.Application.Queries.GetOrders;

public sealed record GetOrdersQuery(OrderStatus? Status = null)
    : IRequest<Result<IReadOnlyList<ServiceOrderDto>>>;
