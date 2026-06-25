namespace Reception.Domain.Enums;

/// <summary>
/// Tracks the lifecycle of a booking from reservation to completion.
/// </summary>
public enum BookingStatus
{
    Pending = 1,        // Created; room in PendingPayment state; 15-min payment window open
    Confirmed = 2,      // Payment received; check-in authorised
    CheckedIn = 3,      // Guest physically in the room
    CheckedOut = 4,     // Guest has departed; bill finalised
    Cancelled = 5,      // Cancelled by guest or auto-released due to payment timeout
    NoShow = 6          // Guest did not arrive and did not cancel
}
