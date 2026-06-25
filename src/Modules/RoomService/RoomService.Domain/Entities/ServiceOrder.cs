using HotelOS.SharedKernel.Abstractions;
using RoomService.Domain.Enums;
using RoomService.Domain.Events;

namespace RoomService.Domain.Entities;

/// <summary>
/// Aggregate Root: ServiceOrder.
///
/// Manages the lifecycle of a food/beverage order placed by a guest.
/// Demonstrates the State Machine pattern enforced at the domain level.
/// The OrderStatus enum is the state; the methods below are the valid transitions.
/// Invalid transitions throw exceptions — the domain protects its own invariants.
/// </summary>
public sealed class ServiceOrder : AggregateRoot<Guid>
{
    public Guid BookingId { get; private set; }
    public string RoomNumber { get; private set; } = null!;
    public OrderStatus Status { get; private set; }
    public decimal TotalPrice { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<OrderLineItem> _lineItems = [];
    public IReadOnlyCollection<OrderLineItem> LineItems => _lineItems.AsReadOnly();

    private ServiceOrder() { }

    public static ServiceOrder Create(Guid bookingId, string roomNumber, IEnumerable<OrderLineItem> items)
    {
        var lineItems = items.ToList();
        if (!lineItems.Any())
            throw new ArgumentException("An order must have at least one item.");

        var order = new ServiceOrder
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            RoomNumber = roomNumber,
            Status = OrderStatus.Received,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        order._lineItems.AddRange(lineItems);
        order.TotalPrice = lineItems.Sum(i => i.UnitPrice * i.Quantity);

        order.RaiseDomainEvent(new OrderStatusChangedEvent(order.Id, order.BookingId, order.RoomNumber, OrderStatus.Received, order.TotalPrice));

        return order;
    }

    // ── State Machine Transitions ─────────────────────────────────────────────

    public void StartPreparing()
    {
        EnsureTransition(OrderStatus.Received, OrderStatus.Preparing);
        Status = OrderStatus.Preparing;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new OrderStatusChangedEvent(Id, BookingId, RoomNumber, Status, TotalPrice));
    }

    public void StartDelivery()
    {
        EnsureTransition(OrderStatus.Preparing, OrderStatus.Delivering);
        Status = OrderStatus.Delivering;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new OrderStatusChangedEvent(Id, BookingId, RoomNumber, Status, TotalPrice));
    }

    /// <summary>
    /// Final state. Raises event so Reception module posts the charge to the active booking.
    /// </summary>
    public void MarkDelivered()
    {
        EnsureTransition(OrderStatus.Delivering, OrderStatus.Delivered);
        Status = OrderStatus.Delivered;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new OrderStatusChangedEvent(Id, BookingId, RoomNumber, Status, TotalPrice));
        RaiseDomainEvent(new OrderDeliveredEvent(Id, BookingId, TotalPrice));
    }

    private void EnsureTransition(OrderStatus expectedCurrent, OrderStatus targetNext)
    {
        if (Status != expectedCurrent)
            throw new InvalidOperationException(
                $"Cannot move to {targetNext}. Expected status {expectedCurrent}, but current is {Status}.");
    }
}

public sealed class OrderLineItem
{
    public string ItemName { get; } = null!;
    public int Quantity { get; }
    public decimal UnitPrice { get; }

    private OrderLineItem() { }

    public OrderLineItem(string itemName, int quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(itemName)) throw new ArgumentException("Item name required.");
        if (quantity < 1) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice));

        ItemName = itemName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
