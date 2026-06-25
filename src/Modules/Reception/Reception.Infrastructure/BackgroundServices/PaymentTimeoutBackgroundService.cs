using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Reception.Application.Interfaces;
using Reception.Domain.Enums;
using Reception.Domain.Events;

namespace Reception.Infrastructure.BackgroundServices;

/// <summary>
/// Hosted service: auto-releases rooms when 15-minute payment windows expire.
///
/// ══ How the Pipeline Works ══
///
///   1. Guest books a room
///      → Room status: Available → PendingPayment
///      → Redis key set: "hotelos:payment-window:{bookingId}" with 15-min TTL
///
///   2a. Guest pays within 15 minutes
///      → ConfirmPaymentCommandHandler deletes the Redis key (CancelPaymentWindowAsync)
///      → Room proceeds to Occupied — no action from this service
///
///   2b. Guest does NOT pay within 15 minutes
///      → Redis fires a keyspace expiry notification on "__keyevent@0__:expired"
///      → THIS SERVICE receives the notification (ExecuteAsync loop)
///      → Creates a DI scope, loads booking + room from DB
///      → Publishes PaymentTimedOutEvent via MediatR
///      → PaymentTimedOutEventHandler: room → Available, booking → Cancelled
///      → UnitOfWork dispatches RoomStatusChangedEvent → SignalR dashboard update
///
/// ══ Why Keyspace Notifications (not PeriodicTimer polling)? ══
/// Keyspace notifications fire within milliseconds of TTL expiry, giving near-instant
/// room re-availability. A PeriodicTimer polling at 1-minute intervals would leave
/// rooms locked up to 1 minute longer than necessary, degrading availability.
///
/// Trade-off accepted: Redis keyspace notifications are "at-most-once" delivery.
/// If this service crashes during the notification, the room stays PendingPayment.
/// Mitigations: (1) idempotent PaymentTimedOutEventHandler, (2) room status visible
/// on dashboard, (3) admin can manually release via API endpoint.
/// For full reliability, add an outbox-pattern periodic sweep.
///
/// ══ DI Scope per Notification ══
/// IBookingRepository and IMediator are Scoped (one per HTTP request). We create a
/// dedicated IServiceScope for each expiry event so each notification gets its own
/// fresh DbContext and transaction — no shared state with HTTP request scopes.
/// </summary>
public sealed class PaymentTimeoutBackgroundService : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentTimeoutBackgroundService> _logger;

    // Must match RedisBookingCacheService.KeyPrefix exactly
    private const string KeyPrefix = "hotelos:payment-window:";
    private const string ExpiredEventsChannel = "__keyevent@0__:expired";

    public PaymentTimeoutBackgroundService(
        IConnectionMultiplexer redis,
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentTimeoutBackgroundService> logger)
    {
        _redis = redis;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // If Redis is not available, log and exit gracefully — do not crash the host.
        if (!_redis.IsConnected)
        {
            _logger.LogWarning(
                "PaymentTimeoutBackgroundService: Redis is not connected. " +
                "Payment timeout auto-release will be inactive until Redis is available.");
            return;
        }

        // ── Enable keyspace notifications on the Redis server ─────────────────
        // "KEA" = Keyspace events (K) + generic commands (E) + expired events (A includes x).
        // This is idempotent — safe to run on every startup.
        // Managed Redis services (Redis Cloud, AWS ElastiCache) may disallow CONFIG SET;
        // in that case configure notify-keyspace-events=KEA in redis.conf instead.
        try
        {
            var server = _redis.GetServers().FirstOrDefault();
            if (server is not null)
                await server.ExecuteAsync("CONFIG", "SET", "notify-keyspace-events", "KEA");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Could not set Redis notify-keyspace-events via CONFIG SET. " +
                "Ensure 'notify-keyspace-events KEA' is set in redis.conf.");
        }

        var subscriber = _redis.GetSubscriber();

        // StackExchange.Redis 2.x uses ChannelMessageQueue for subscriptions.
        // The queue is an IAsyncEnumerable; we iterate it below — each message
        // spawns a separate task to avoid blocking the queue reader.
        var queue = await subscriber.SubscribeAsync(
            RedisChannel.Literal(ExpiredEventsChannel));

        _logger.LogInformation(
            "PaymentTimeoutBackgroundService started — listening on Redis channel '{Channel}'.",
            ExpiredEventsChannel);

        // Process each expiry notification as it arrives.
        // The await foreach holds this loop alive for the application's lifetime.
        await foreach (var message in queue.WithCancellation(stoppingToken))
        {
            // Fire-and-forget per notification. Exceptions are caught inside
            // HandleKeyExpiredAsync so a single failure never breaks the loop.
            _ = HandleKeyExpiredAsync(message.Message.ToString(), stoppingToken);
        }
    }

    /// <summary>
    /// Processes a single Redis key-expiry event for a payment window key.
    /// Runs in its own DI scope so it gets a fresh DbContext per event.
    /// </summary>
    private async Task HandleKeyExpiredAsync(string keyName, CancellationToken ct)
    {
        // Filter: only process our payment window keys, ignore all other TTL events
        if (!keyName.StartsWith(KeyPrefix, StringComparison.Ordinal))
            return;

        // Extract bookingId from the key: "hotelos:payment-window:{bookingId}"
        var bookingIdString = keyName[KeyPrefix.Length..]; // String slice — no Span needed
        if (!Guid.TryParse(bookingIdString, out var bookingId))
        {
            _logger.LogWarning("Received malformed payment-window key: {Key}", keyName);
            return;
        }

        _logger.LogInformation(
            "Payment window expired for booking {BookingId} — initiating auto-release.",
            bookingId);

        // Fresh scope = fresh DbContext = fresh EF Core change tracker.
        // This is required because BackgroundService is a Singleton but repositories
        // are Scoped — they cannot be injected directly into a Singleton.
        await using var scope = _scopeFactory.CreateAsyncScope();
        try
        {
            var bookingRepo = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
            var roomRepo    = scope.ServiceProvider.GetRequiredService<IRoomRepository>();
            var mediator    = scope.ServiceProvider.GetRequiredService<IMediator>();

            // Load the booking to retrieve the roomId
            var booking = await bookingRepo.GetByIdAsync(bookingId, ct);
            if (booking is null)
            {
                // Already cleaned up by another path (e.g., admin cancel)
                _logger.LogWarning(
                    "PaymentTimeout: booking {BookingId} not found — already cleaned up.",
                    bookingId);
                return;
            }

            // Guard: payment may have arrived in the milliseconds between TTL expiry
            // and this handler running. PaymentTimedOutEventHandler also guards this,
            // but a quick status check here avoids unnecessary DB/domain work.
            if (booking.Status != BookingStatus.Pending)
            {
                _logger.LogInformation(
                    "PaymentTimeout: booking {BookingId} already in status {Status} — skipping.",
                    bookingId, booking.Status);
                return;
            }

            // Load the room to get the RoomNumber string required by PaymentTimedOutEvent
            var room = await roomRepo.GetByIdAsync(booking.RoomId, ct);
            if (room is null)
            {
                _logger.LogError(
                    "PaymentTimeout: room {RoomId} for booking {BookingId} not found in DB.",
                    booking.RoomId, bookingId);
                return;
            }

            // Publish the domain event.
            // PaymentTimedOutEventHandler (Reception.Application) handles:
            //   room.ReleaseFromPaymentHold() → Available
            //   booking.Cancel() → Cancelled
            //   UoW.SaveChangesAsync() → dispatches RoomStatusChangedEvent → SignalR
            await mediator.Publish(
                new PaymentTimedOutEvent(bookingId, room.Id, room.RoomNumber.Value),
                ct);

            _logger.LogInformation(
                "PaymentTimedOutEvent published for booking {BookingId}, room {RoomNumber}.",
                bookingId, room.RoomNumber.Value);
        }
        catch (OperationCanceledException)
        {
            // Application is shutting down — expected, not an error
        }
        catch (Exception ex)
        {
            // Log and swallow: one failed event must not crash the BackgroundService loop.
            // The room remains PendingPayment and can be manually released via admin API.
            _logger.LogError(ex,
                "Error processing payment timeout for booking {BookingId}.", bookingId);
        }
    }
}
