using HotelOS.SharedKernel.Common;

namespace Reception.Domain.Events;

/// <summary>
/// Published when a booking is cancelled — either by the guest or automatically
/// by the BackgroundService after the 15-minute payment TTL expires in Redis.
/// </summary>
public sealed record BookingCancelledEvent(
    Guid BookingId,
    Guid RoomId) : BaseEvent;
