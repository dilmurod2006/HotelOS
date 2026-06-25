namespace Reception.Application.Interfaces;

/// <summary>
/// Manages the Redis keys that track the 15-minute payment window for bookings.
///
/// Key format:  "hotelos:payment-window:{bookingId}"
/// Value:       The roomId (as string) so the BackgroundService knows which room
///              to release when the TTL expires.
/// TTL:         15 minutes (configurable via appsettings).
///
/// When the key expires, Redis fires a keyspace notification.
/// The BackgroundService (Step 3) listens to these events and calls
/// room.ReleaseFromPaymentHold() + publishes PaymentTimedOutEvent via MediatR.
/// </summary>
public interface IRedisBookingCache
{
    /// <summary>Sets the 15-minute payment window key in Redis.</summary>
    Task SetPaymentWindowAsync(Guid bookingId, Guid roomId, TimeSpan ttl, CancellationToken ct = default);

    /// <summary>Explicitly removes the payment window key (called on successful payment).</summary>
    Task CancelPaymentWindowAsync(Guid bookingId, CancellationToken ct = default);

    /// <summary>Returns the roomId string stored in the payment window key, or null if expired/missing.</summary>
    Task<string?> GetPaymentWindowRoomIdAsync(Guid bookingId, CancellationToken ct = default);
}
