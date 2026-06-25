using HotelOS.SharedKernel.Common;
using MediatR;
using Reception.Application.DTOs;
using Reception.Domain.Enums;

namespace Reception.Application.Queries.GetAvailableRooms;

public sealed record GetAvailableRoomsQuery(
    RoomType? Type = null,
    int? Floor = null) : IRequest<Result<IReadOnlyList<RoomDto>>>;
