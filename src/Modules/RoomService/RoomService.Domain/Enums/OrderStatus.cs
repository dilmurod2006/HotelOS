namespace RoomService.Domain.Enums;

/// <summary>
/// State machine for room service orders — strict forward-only transitions.
/// Algorithm: Received → Preparing → Delivering → Delivered.
/// No state can be skipped. No backward transitions are permitted.
/// This models a real kitchen workflow where each step has physical meaning.
/// </summary>
public enum OrderStatus
{
    Received = 1,    // Order placed by guest; kitchen notified
    Preparing = 2,   // Kitchen actively cooking/assembling the order
    Delivering = 3,  // Staff picked up order, on the way to the room
    Delivered = 4    // Guest received the order; charge posted to booking
}
