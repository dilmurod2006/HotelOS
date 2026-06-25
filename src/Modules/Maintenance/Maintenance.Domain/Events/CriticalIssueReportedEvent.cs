using HotelOS.SharedKernel.Common;

namespace Maintenance.Domain.Events;

/// <summary>
/// Raised when a Critical maintenance issue is reported.
/// Reception subscribes to this to call Room.PlaceUnderMaintenance(),
/// preventing any new bookings for that room until the issue is resolved.
/// </summary>
public sealed record CriticalIssueReportedEvent(
    Guid IssueId,
    Guid RoomId,
    string RoomNumber) : BaseEvent;
