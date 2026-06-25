using StackExchange.Redis;
using Reception.Application.Interfaces;

namespace Reception.Infrastructure.Redis;

/// <summary>
/// StackExchange.Redis implementation of IRedisBookingCache.
///
/// Key naming convention: "hotelos:payment-window:{bookingId}"
/// Value: roomId as a string (for correlation if ever needed for debugging)
/// TTL: 15 minutes (configured by the Application layer, passed in at call-site)
///
/// ══ Why the value stores roomId ══
/// When the TTL fires, Redis keyspace notifications provide only the KEY name —
/// not the value (the value is already deleted at expiry time).
/// The BackgroundService therefore queries the DB for the booking (by bookingId
/// extracted from the key) to retrieve the roomId and roomNumber.
/// The stored roomId here is available for debugging and for explicit lookups via
/// GetPaymentWindowRoomIdAsync while the key is still alive.
/// </summary>
internal sealed class RedisBookingCacheService : IRedisBookingCache
{
    private readonly IDatabase _db;

    internal const string KeyPrefix = "hotelos:payment-window:";

    public RedisBookingCacheService(IConnectionMultiplexer connectionMultiplexer)
        => _db = connectionMultiplexer.GetDatabase();

    public async Task SetPaymentWindowAsync(
        Guid bookingId, Guid roomId, TimeSpan ttl, CancellationToken ct = default)
    {
        var key = $"{KeyPrefix}{bookingId}";
        await _db.StringSetAsync(key, roomId.ToString(), ttl);
    }

    public async Task CancelPaymentWindowAsync(
        Guid bookingId, CancellationToken ct = default)
        => await _db.KeyDeleteAsync($"{KeyPrefix}{bookingId}");

    public async Task<string?> GetPaymentWindowRoomIdAsync(
        Guid bookingId, CancellationToken ct = default)
    {
        var value = await _db.StringGetAsync($"{KeyPrefix}{bookingId}");
        return value.HasValue ? value.ToString() : null;
    }
}
