using HotelOS.SharedKernel.Common;

namespace Housekeeping.Domain.Events;

/// <summary>
/// Published when a cleaner marks a room as fully cleaned.
/// Reception subscribes to this to update the room status to Clean and record LastCleanedAt.
/// The SignalR hub also listens to push the status change to the dashboard.
/// </summary>
public sealed record CleaningCompletedEvent(
    Guid TaskId,
    Guid RoomId,
    string RoomNumber) : BaseEvent;
