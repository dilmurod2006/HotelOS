using HotelOS.SharedKernel.Common;

namespace Maintenance.Domain.Events;

/// <summary>
/// Raised when a Critical issue is resolved.
/// Reception subscribes to this to call Room.ReturnToService().
/// </summary>
public sealed record CriticalIssueResolvedEvent(
    Guid IssueId,
    Guid RoomId,
    string RoomNumber) : BaseEvent;
