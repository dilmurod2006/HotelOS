using MediatR;
using HotelOS.SharedKernel.Common;
using Reception.Application.DTOs;
using Reception.Domain.Enums;

namespace Reception.Application.Commands.CreateBooking;

/// <summary>
/// MediatR Command: CreateBooking.
///
/// A Command in CQRS represents an intent to change state.
/// It carries everything the system needs to execute the booking process:
/// who is booking (GuestId), what they want (RoomType, floor, proximity),
/// and when they want to stay (CheckIn / CheckOut dates).
///
/// The handler returns Result{BookingDto} — a success containing the confirmed
/// booking details, or a failure with an Error describing why it failed.
/// </summary>
public sealed record CreateBookingCommand(
    Guid GuestId,
    RoomType RequestedRoomType,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int? PreferredFloor,
    ProximityPreference ProximityPreference
) : IRequest<Result<BookingDto>>;
