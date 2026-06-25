using RedLockNet;
using RedLockNet.SERedis;
using Reception.Application.Interfaces;

namespace Reception.Infrastructure.Redis;

/// <summary>
/// RedLock.net implementation of IDistributedLockService.
///
/// RedLock is the Redis community's recommended algorithm for distributed locking.
/// It acquires the same key across N Redis nodes simultaneously (N=1 in development).
/// A lock is considered "acquired" only when a majority of nodes confirm it — this
/// prevents a single Redis node failure from causing two clients to hold the same lock.
///
/// In this system, the lock key is "hotelos:room-lock:{roomId}".
/// waitTime = Zero: do not block if the lock is held — fail immediately.
/// This keeps the HTTP request responsive; the client receives a clear "try again"
/// response rather than a long pause waiting for the lock to become available.
/// </summary>
internal sealed class RedisDistributedLockService : IDistributedLockService
{
    private readonly RedLockFactory _factory;

    public RedisDistributedLockService(RedLockFactory factory) => _factory = factory;

    public async Task<IDistributedLock?> TryAcquireAsync(
        string resource,
        TimeSpan lockExpiry,
        CancellationToken ct = default)
    {
        var redLock = await _factory.CreateLockAsync(
            resource: resource,
            expiryTime: lockExpiry,
            waitTime: TimeSpan.Zero,   // Fail fast — never block the request
            retryTime: TimeSpan.Zero,  // No retries; caller handles "not acquired"
            cancellationToken: ct);

        return new RedLockAdapter(redLock);
    }

    /// <summary>
    /// Wraps IRedLock (IDisposable) in our IDistributedLock (IAsyncDisposable).
    /// Disposing the adapter immediately releases the Redis lock key — the room
    /// becomes available again without waiting for the 30-second expiry TTL.
    /// </summary>
    private sealed class RedLockAdapter : IDistributedLock
    {
        private readonly IRedLock _inner;

        public RedLockAdapter(IRedLock inner) => _inner = inner;

        public bool IsAcquired => _inner.IsAcquired;

        public ValueTask DisposeAsync()
        {
            _inner.Dispose(); // Releases the lock in Redis synchronously
            return ValueTask.CompletedTask;
        }
    }
}
