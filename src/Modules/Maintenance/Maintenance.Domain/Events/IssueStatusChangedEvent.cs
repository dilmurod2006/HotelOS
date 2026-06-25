using HotelOS.SharedKernel.Common;
using Maintenance.Domain.Enums;

namespace Maintenance.Domain.Events;

/// <summary>
/// Raised on every maintenance issue status change.
/// The SignalR hub listens to push real-time maintenance queue updates to the dashboard.
/// </summary>
public sealed record IssueStatusChangedEvent(
    Guid IssueId,
    string RoomNumber,
    IssueStatus NewStatus,
    IssueUrgency Urgency) : BaseEvent;
