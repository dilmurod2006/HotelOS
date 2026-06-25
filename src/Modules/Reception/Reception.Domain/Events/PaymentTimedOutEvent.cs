using HotelOS.SharedKernel.Common;

namespace Reception.Domain.Events;

/// <summary>
/// Raised by the Redis TTL BackgroundService when a booking's 15-minute payment
/// window expires. Triggers room release and SignalR dashboard notification.
/// This event is the bridge between Redis infrastructure and the domain.
/// </summary>
public sealed record PaymentTimedOutEvent(
    Guid BookingId,
    Guid RoomId,
    string RoomNumber) : BaseEvent;
