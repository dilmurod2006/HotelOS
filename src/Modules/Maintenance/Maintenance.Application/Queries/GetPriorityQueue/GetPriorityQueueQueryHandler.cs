using HotelOS.SharedKernel.Common;
using Maintenance.Application.Commands.ReportIssue;
using Maintenance.Application.DTOs;
using Maintenance.Application.Interfaces;
using MediatR;

namespace Maintenance.Application.Queries.GetPriorityQueue;

/// <summary>
/// PRIORITY QUEUE ALGORITHM
/// ─────────────────────────────────────────────────────────────────────────
/// 1. Repository returns all non-Resolved issues already sorted at the DB level:
///       PRIMARY sort  : Urgency ASC  →  Critical(1) → High(2) → Normal(3) → Low(4)
///       SECONDARY sort: CreatedAt ASC → FIFO within the same urgency band
/// 2. Each issue is assigned a 1-based PriorityRank (rank 1 = next technician assignment).
/// 3. PriorityRank is ephemeral — it reflects the queue snapshot at query time.
/// ─────────────────────────────────────────────────────────────────────────
/// Why ascending numeric sort works: the enum is defined as
///   Critical = 1, High = 2, Normal = 3, Low = 4
/// so ORDER BY (int)Urgency ASC naturally surfaces the most critical issue first.
/// </summary>
public sealed class GetPriorityQueueQueryHandler
    : IRequestHandler<GetPriorityQueueQuery, Result<IReadOnlyList<MaintenanceIssueDto>>>
{
    private readonly IMaintenanceIssueRepository _repository;

    public GetPriorityQueueQueryHandler(IMaintenanceIssueRepository repository)
        => _repository = repository;

    public async Task<Result<IReadOnlyList<MaintenanceIssueDto>>> Handle(
        GetPriorityQueueQuery query, CancellationToken cancellationToken)
    {
        // DB-sorted list: already ordered by Urgency ASC, CreatedAt ASC
        var issues = await _repository.GetPriorityQueueAsync(cancellationToken);

        // Assign 1-based rank: position in queue determines assignment order
        var dto = issues
            .Select((issue, index) =>
                ReportIssueCommandHandler.MapToDto(issue, priorityRank: index + 1))
            .ToList()
            .AsReadOnly();

        return Result<IReadOnlyList<MaintenanceIssueDto>>.Success(dto);
    }
}
