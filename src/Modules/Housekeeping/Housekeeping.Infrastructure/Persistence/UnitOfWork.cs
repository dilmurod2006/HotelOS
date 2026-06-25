using HotelOS.SharedKernel.Abstractions;
using Housekeeping.Application.Interfaces;
using MediatR;

namespace Housekeeping.Infrastructure.Persistence;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly HousekeepingDbContext _context;
    private readonly IMediator _mediator;

    public UnitOfWork(HousekeepingDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var aggregates = _context.ChangeTracker.Entries()
            .Where(e => e.Entity is AggregateRoot<Guid> a && a.DomainEvents.Any())
            .Select(e => (AggregateRoot<Guid>)e.Entity)
            .ToList();

        var domainEvents = aggregates.SelectMany(a => a.DomainEvents).ToList();

        var savedCount = await _context.SaveChangesAsync(ct);

        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        foreach (var domainEvent in domainEvents)
            await _mediator.Publish(domainEvent, ct);

        return savedCount;
    }
}
