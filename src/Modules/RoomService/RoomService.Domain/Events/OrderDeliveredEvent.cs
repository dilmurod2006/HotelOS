using HotelOS.SharedKernel.Common;

namespace RoomService.Domain.Events;

/// <summary>
/// Published when an order reaches Delivered status.
/// Reception subscribes to this to add the order total to the guest's booking charges.
/// This decouples billing from order management — Room Service never calls Reception directly.
/// </summary>
public sealed record OrderDeliveredEvent(
    Guid OrderId,
    Guid BookingId,
    decimal Amount) : BaseEvent;
