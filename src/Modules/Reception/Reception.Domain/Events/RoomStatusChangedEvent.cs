using HotelOS.SharedKernel.Common;
using Reception.Domain.Enums;

namespace Reception.Domain.Events;

/// <summary>
/// Domain Event published on every room status change.
/// The SignalR hub subscribes to this event's handler to push real-time
/// updates to all connected dashboard clients.
/// </summary>
public sealed record RoomStatusChangedEvent(
    Guid RoomId,
    string RoomNumber,
    RoomStatus NewStatus) : BaseEvent;
