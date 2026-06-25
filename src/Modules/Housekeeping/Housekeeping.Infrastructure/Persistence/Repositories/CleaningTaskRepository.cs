using Housekeeping.Application.Interfaces;
using Housekeeping.Domain.Entities;
using Housekeeping.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Housekeeping.Infrastructure.Persistence.Repositories;

internal sealed class CleaningTaskRepository : ICleaningTaskRepository
{
    private readonly HousekeepingDbContext _context;

    public CleaningTaskRepository(HousekeepingDbContext context) => _context = context;

    public async Task<CleaningTask?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.CleaningTasks.FindAsync([id], ct);

    public async Task<IReadOnlyList<CleaningTask>> GetAllAsync(CancellationToken ct = default)
        => await _context.CleaningTasks
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CleaningTask>> GetByStatusAsync(
        CleaningTaskStatus status, CancellationToken ct = default)
        => await _context.CleaningTasks
            .Where(t => t.Status == status)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task<CleaningTask?> GetActiveTaskForRoomAsync(Guid roomId, CancellationToken ct = default)
        => await _context.CleaningTasks
            .Where(t => t.RoomId == roomId && t.Status != CleaningTaskStatus.Completed)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(CleaningTask task, CancellationToken ct = default)
        => await _context.CleaningTasks.AddAsync(task, ct);

    public Task UpdateAsync(CleaningTask task, CancellationToken ct = default)
    {
        _context.CleaningTasks.Update(task);
        return Task.CompletedTask;
    }
}
