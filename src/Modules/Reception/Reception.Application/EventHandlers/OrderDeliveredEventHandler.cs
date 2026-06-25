using MediatR;
using Microsoft.Extensions.Logging;
using RoomService.Domain.Events;
using Reception.Application.Interfaces;
using Reception.Domain.ValueObjects;

namespace Reception.Application.EventHandlers;

/// <summary>
/// Cross-module Event Handler: RoomService → Reception.
///
/// When a Room Service order is delivered, this handler posts the charge to the
/// active booking — so the amount appears on the guest's final bill at check-out.
///
/// Event-driven decoupling: RoomService publishes OrderDeliveredEvent with the
/// Amount. Reception listens and calls booking.AddRoomServiceCharge(). Neither
/// module references the other's code — only the event contract (in SharedKernel)
/// is shared.
/// </summary>
public sealed class OrderDeliveredEventHandler
    : INotificationHandler<OrderDeliveredEvent>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderDeliveredEventHandler> _logger;

    public OrderDeliveredEventHandler(
        IBookingRepository bookingRepository,
        IUnitOfWork unitOfWork,
        ILogger<OrderDeliveredEventHandler> logger)
    {
        _bookingRepository = bookingRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(OrderDeliveredEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "OrderDelivered event: posting {Amount} charge to booking {BookingId}",
            notification.Amount, notification.BookingId);

        var booking = await _bookingRepository.GetByIdAsync(notification.BookingId, cancellationToken);
        if (booking is null)
        {
            _logger.LogError(
                "Cannot post room service charge: Booking {BookingId} not found.",
                notification.BookingId);
            return;
        }

        // Wrap the raw decimal amount in a Money value object with default currency.
        // A future enhancement: the event could carry the currency code.
        var charge = Money.Of(notification.Amount);
        booking.AddRoomServiceCharge(charge);

        await _bookingRepository.UpdateAsync(booking, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Room service charge {Amount} posted to booking {BookingId}. " +
            "New room service total: {Total}",
            notification.Amount, notification.BookingId, booking.RoomServiceCharges);
    }
}
