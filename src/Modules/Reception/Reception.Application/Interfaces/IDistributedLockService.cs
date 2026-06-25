namespace Reception.Application.Interfaces;

/// <summary>
/// Abstraction over RedLock.net (Redis distributed lock).
///
/// WHY a distributed lock here?
/// In a concurrent system, two HTTP requests for the last available room can
/// arrive simultaneously. EF Core's optimistic concurrency (RowVersion) catches
/// the SECOND save, but the lock prevents both requests from even entering the
/// booking logic at the same time — this is the FIRST line of defence.
///
/// The resource string is typically "room-lock:{roomId}" so only one booking
/// attempt per specific room can hold the lock at any moment.
/// </summary>
public interface IDistributedLockService
{
    /// <summary>
    /// Attempts to acquire an exclusive lock on <paramref name="resource"/>.
    /// Returns null if the lock could not be acquired within the attempt window.
    /// The caller MUST dispose the returned lock to release it — use <c>await using</c>.
    /// </summary>
    Task<IDistributedLock?> TryAcquireAsync(
        string resource,
        TimeSpan lockExpiry,
        CancellationToken ct = default);
}

/// <summary>
/// Represents an acquired distributed lock. Disposing releases it from Redis.
/// Implemented by RedLock.net's IRedLock wrapper in Infrastructure.
/// </summary>
public interface IDistributedLock : IAsyncDisposable
{
    bool IsAcquired { get; }
}
