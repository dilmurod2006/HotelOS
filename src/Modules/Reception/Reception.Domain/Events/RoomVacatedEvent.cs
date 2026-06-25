using HotelOS.SharedKernel.Common;

namespace Reception.Domain.Events;

/// <summary>
/// Domain Event published when a guest checks out.
///
/// This event crosses the Reception → Housekeeping module boundary.
/// Reception raises it; Housekeeping subscribes to it via MediatR INotificationHandler.
/// Reception has zero knowledge of who handles this event — that is the key benefit
/// of event-driven communication in a Modular Monolith.
/// </summary>
public sealed record RoomVacatedEvent(
    Guid RoomId,
    string RoomNumber,
    int Floor) : BaseEvent;
