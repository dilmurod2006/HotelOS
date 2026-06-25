using MediatR;
using Microsoft.EntityFrameworkCore;
using HotelOS.SharedKernel.Abstractions;
using HotelOS.SharedKernel.Exceptions;
using Reception.Application.Interfaces;
using Reception.Domain.Entities;

namespace Reception.Infrastructure.Persistence;

/// <summary>
/// Unit of Work implementation for the Reception module.
///
/// ══ Domain Event Dispatch Order ══
/// Events are dispatched AFTER the database commit, not before.
/// This guarantees that cross-module side effects (e.g., Housekeeping creating a
/// CleaningTask, SignalR pushing a dashboard update) only happen when the triggering
/// state change is durably persisted. If SaveChangesAsync throws, events are NOT
/// dispatched — the system stays consistent.
///
/// ══ RowVersion Refresh ══
/// PostgreSQL has no native rowversion column (unlike SQL Server). We simulate the
/// same behaviour by generating Guid.NewGuid().ToByteArray() in the UoW before
/// every Add/Modify save. EF Core stores the NEW bytes in the DB and remembers the
/// ORIGINAL bytes in its change tracker. The next UPDATE for that entity includes
/// WHERE RowVersion = @originalBytes — if another process already changed the row,
/// the byte array differs → 0 rows affected → DbUpdateConcurrencyException.
///
/// ══ Error Wrapping ══
/// The Application layer must not reference Microsoft.EntityFrameworkCore.
/// DbUpdateConcurrencyException (from EF Core) is caught here and re-thrown as
/// ConcurrencyConflictException (from HotelOS.SharedKernel) which the handler catches.
/// </summary>
internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly ReceptionDbContext _context;
    private readonly IMediator _mediator;

    public UnitOfWork(ReceptionDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // ── Step 1: Refresh RowVersion on every Room being written ────────────
        // New Guid bytes on each write make the token unique and detectable.
        // entry.Property(r => r.RowVersion).CurrentValue sets the "to-be-written"
        // value; EF Core automatically uses the "original" value in the WHERE clause.
        foreach (var entry in _context.ChangeTracker.Entries<Room>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Property(r => r.RowVersion).CurrentValue = Guid.NewGuid().ToByteArray();
        }

        // ── Step 2: Collect domain events BEFORE the DB write ─────────────────
        // We harvest events now because SaveChangesAsync might detach entities or
        // the collection could be mutated during commit. All Reception aggregates
        // extend AggregateRoot<Guid> — this cast is safe within the Reception module.
        var aggregates = _context.ChangeTracker
            .Entries()
            .Where(e => e.Entity is AggregateRoot<Guid> a && a.DomainEvents.Any())
            .Select(e => (AggregateRoot<Guid>)e.Entity)
            .ToList();

        var domainEvents = aggregates
            .SelectMany(a => a.DomainEvents)
            .ToList();

        // ── Step 3: Commit to PostgreSQL ──────────────────────────────────────
        int savedCount;
        try
        {
            savedCount = await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Translate ORM exception to domain exception so Application stays clean.
            // The outer CreateBookingCommandHandler catches ConcurrencyConflictException.
            throw new ConcurrencyConflictException(
                "A concurrency conflict was detected. Please retry the operation.", ex);
        }

        // ── Step 4: Clear events AFTER successful commit ──────────────────────
        // Only clear if the commit succeeded — if SaveChanges threw, events remain
        // on the aggregate and could be retried.
        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        // ── Step 5: Dispatch events AFTER commit ──────────────────────────────
        // RoomStatusChangedEvent → SignalR hub (real-time dashboard)
        // RoomVacatedEvent       → Housekeeping module creates a CleaningTask
        // BookingCancelledEvent  → any audit/notification subscribers
        foreach (var domainEvent in domainEvents)
            await _mediator.Publish(domainEvent, ct);

        return savedCount;
    }
}
