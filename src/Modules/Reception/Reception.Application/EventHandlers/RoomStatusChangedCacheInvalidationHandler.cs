using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Reception.Application.Queries.GetAvailableRooms;
using Reception.Domain.Events;

namespace Reception.Application.EventHandlers;

/// <summary>
/// Invalidates the available-rooms Redis cache whenever any room's status changes.
///
/// Why: A room moving from Available → HeldForPayment (or any status transition)
/// changes the set of bookable rooms. The 30-second TTL provides eventual consistency
/// under normal load; this handler provides immediate consistency on status changes.
/// The next request after invalidation will repopulate the cache from PostgreSQL.
/// </summary>
public sealed class RoomStatusChangedCacheInvalidationHandler
    : INotificationHandler<RoomStatusChangedEvent>
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RoomStatusChangedCacheInvalidationHandler> _logger;

    public RoomStatusChangedCacheInvalidationHandler(
        IDistributedCache cache,
        ILogger<RoomStatusChangedCacheInvalidationHandler> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task Handle(RoomStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync(GetAvailableRoomsQueryHandler.CacheKey, cancellationToken);

        _logger.LogDebug(
            "Available-rooms cache invalidated. Room {RoomNumber} changed to {Status}.",
            notification.RoomNumber, notification.NewStatus);
    }
}
