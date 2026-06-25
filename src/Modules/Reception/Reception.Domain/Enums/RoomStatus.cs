namespace Reception.Domain.Enums;

/// <summary>
/// Represents the lifecycle state of a hotel room.
/// The state machine flow is:
///   Available → Occupied (check-in)
///   Available → PendingPayment (booking created, awaiting payment — 15-min TTL)
///   PendingPayment → Available (TTL expired, auto-released by BackgroundService)
///   PendingPayment → Occupied (payment confirmed, check-in completes)
///   Occupied → Dirty (check-out)
///   Dirty → Cleaning (Housekeeping picks up task)
///   Cleaning → Clean (Housekeeping marks complete)
///   Clean → Available (after inspection, made bookable)
///   Any → UnderMaintenance (Maintenance module flags the room)
///   UnderMaintenance → Available (Maintenance marks issue resolved)
/// </summary>
public enum RoomStatus
{
    Available = 1,
    PendingPayment = 2,
    Occupied = 3,
    Dirty = 4,
    Cleaning = 5,
    Clean = 6,
    UnderMaintenance = 7
}
