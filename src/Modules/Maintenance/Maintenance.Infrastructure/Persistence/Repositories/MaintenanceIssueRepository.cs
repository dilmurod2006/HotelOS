using Maintenance.Application.Interfaces;
using Maintenance.Domain.Entities;
using Maintenance.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Maintenance.Infrastructure.Persistence.Repositories;

internal sealed class MaintenanceIssueRepository : IMaintenanceIssueRepository
{
    private readonly MaintenanceDbContext _context;

    public MaintenanceIssueRepository(MaintenanceDbContext context) => _context = context;

    public async Task<MaintenanceIssue?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.MaintenanceIssues.FindAsync([id], ct);

    /// <summary>
    /// PRIORITY QUEUE ALGORITHM — executed at the database level.
    ///
    /// Filters: Status != Resolved (only actionable issues)
    /// Sort 1:  Urgency ASC  → Critical(1) → High(2) → Normal(3) → Low(4)
    /// Sort 2:  CreatedAt ASC → FIFO within the same urgency band
    ///
    /// The composite index IX_MaintenanceIssues_PriorityQueue covers this query exactly,
    /// so PostgreSQL returns results in O(1) scan order without a sort step.
    /// </summary>
    public async Task<IReadOnlyList<MaintenanceIssue>> GetPriorityQueueAsync(CancellationToken ct = default)
        => await _context.MaintenanceIssues
            .Where(i => i.Status != IssueStatus.Resolved)
            .OrderBy(i => i.Urgency)
            .ThenBy(i => i.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<MaintenanceIssue>> GetByRoomIdAsync(
        Guid roomId, CancellationToken ct = default)
        => await _context.MaintenanceIssues
            .Where(i => i.RoomId == roomId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(MaintenanceIssue issue, CancellationToken ct = default)
        => await _context.MaintenanceIssues.AddAsync(issue, ct);

    public Task UpdateAsync(MaintenanceIssue issue, CancellationToken ct = default)
    {
        _context.MaintenanceIssues.Update(issue);
        return Task.CompletedTask;
    }
}
