using HotelOS.SharedKernel.Common;
using MediatR;
using RoomService.Application.DTOs;

namespace RoomService.Application.Commands.AdvanceOrderStatus;

public sealed record AdvanceOrderStatusCommand(Guid OrderId) : IRequest<Result<ServiceOrderDto>>;
