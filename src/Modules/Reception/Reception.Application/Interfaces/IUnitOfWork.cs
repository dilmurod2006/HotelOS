namespace Reception.Application.Interfaces;

/// <summary>
/// Unit of Work pattern: wraps a database transaction so multiple repository
/// operations commit atomically. Critical for the two-phase booking process
/// where we update Room status AND create a Booking in a single transaction.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Saves all pending changes and dispatches accumulated domain events
    /// AFTER the database transaction commits — guaranteeing events only
    /// fire when persistence is confirmed.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
