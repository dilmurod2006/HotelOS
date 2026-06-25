using Housekeeping.Domain.Entities;
using Housekeeping.Domain.Enums;

namespace Housekeeping.Application.Interfaces;

public interface ICleaningTaskRepository
{
    Task<CleaningTask?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<CleaningTask>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CleaningTask>> GetByStatusAsync(CleaningTaskStatus status, CancellationToken ct = default);
    Task<CleaningTask?> GetActiveTaskForRoomAsync(Guid roomId, CancellationToken ct = default);
    Task AddAsync(CleaningTask task, CancellationToken ct = default);
    Task UpdateAsync(CleaningTask task, CancellationToken ct = default);
}
