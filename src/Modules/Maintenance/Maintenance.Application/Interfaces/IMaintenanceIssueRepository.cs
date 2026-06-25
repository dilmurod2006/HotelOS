using Maintenance.Domain.Entities;
using Maintenance.Domain.Enums;

namespace Maintenance.Application.Interfaces;

public interface IMaintenanceIssueRepository
{
    Task<MaintenanceIssue?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns all non-resolved issues pre-sorted by the Priority Queue algorithm:
    ///   PRIMARY:   IssueUrgency ASC  (Critical=1 first, Low=4 last)
    ///   SECONDARY: CreatedAt ASC     (oldest first within the same urgency — FIFO)
    /// </summary>
    Task<IReadOnlyList<MaintenanceIssue>> GetPriorityQueueAsync(CancellationToken ct = default);

    Task<IReadOnlyList<MaintenanceIssue>> GetByRoomIdAsync(Guid roomId, CancellationToken ct = default);
    Task AddAsync(MaintenanceIssue issue, CancellationToken ct = default);
    Task UpdateAsync(MaintenanceIssue issue, CancellationToken ct = default);
}
