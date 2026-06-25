namespace Maintenance.Application.DTOs;

/// <summary>
/// Represents a single item in the Priority Queue response.
/// PriorityRank is 1-based: rank 1 = most urgent, assigned first.
/// </summary>
public sealed record MaintenanceIssueDto(
    Guid Id,
    Guid RoomId,
    string RoomNumber,
    string Description,
    string Urgency,
    int UrgencyLevel,     // Numeric sort key: 1=Critical, 2=High, 3=Normal, 4=Low
    string Status,
    int PriorityRank,     // Position in queue (1 = next to be worked on)
    Guid? AssignedTechnicianId,
    string? ResolutionNotes,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ResolvedAt);
