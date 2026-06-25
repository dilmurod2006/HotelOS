using HotelOS.SharedKernel.Common;
using RoomService.Domain.Enums;

namespace RoomService.Domain.Events;

/// <summary>
/// Raised on every order state transition.
/// The SignalR hub listens to push real-time order tracking to the dashboard.
/// </summary>
public sealed record OrderStatusChangedEvent(
    Guid OrderId,
    Guid BookingId,
    string RoomNumber,
    OrderStatus NewStatus,
    decimal TotalPrice) : BaseEvent;
