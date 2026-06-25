using HotelOS.SharedKernel.Common;

namespace Housekeeping.Domain.Events;

/// <summary>
/// Published when a cleaner begins work on a room.
/// Reception subscribes to this to update the room status to Cleaning.
/// </summary>
public sealed record CleaningStartedEvent(
    Guid TaskId,
    Guid RoomId,
    string RoomNumber) : BaseEvent;
