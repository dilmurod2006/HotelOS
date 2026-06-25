namespace HotelOS.SharedKernel.Exceptions;

/// <summary>
/// Thrown by Infrastructure's UnitOfWork when EF Core detects a RowVersion
/// mismatch (DbUpdateConcurrencyException). Re-wrapping it here keeps the
/// Application layer free of any EF Core assembly references — Application
/// only depends on this SharedKernel exception, not on the ORM.
///
/// Clean Architecture boundary: Infrastructure → throws ConcurrencyConflictException
///                               Application → catches ConcurrencyConflictException
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException()
        : base("A concurrency conflict was detected. The entity was modified by another request.") { }

    public ConcurrencyConflictException(string message)
        : base(message) { }

    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException) { }
}
