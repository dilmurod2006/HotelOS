using MediatR;
using Microsoft.Extensions.Logging;
using Reception.Application.Interfaces;
using Reception.Domain.Events;

namespace Reception.Application.EventHandlers;

/// <summary>
/// Event Handler: PaymentTimedOut — triggered by the Redis TTL BackgroundService (Step 3).
///
/// BTEC payment timeout flow:
///   1. Guest books a room → room status = PendingPayment, 15-min Redis TTL set.
///   2. Guest does NOT pay within 15 minutes.
///   3. Redis fires a keyspace expiry notification.
///   4. BackgroundService (Step 3) publishes PaymentTimedOutEvent via MediatR.
///   5. THIS HANDLER executes: cancels the booking, releases the room to Available.
///   6. The room.ReleaseFromPaymentHold() call raises RoomStatusChangedEvent.
///   7. SignalR hub pushes "Room 204 is now Available" to all dashboard clients.
///
/// This entire chain happens automatically — no human intervention required.
/// </summary>
public sealed class PaymentTimedOutEventHandler
    : INotificationHandler<PaymentTimedOutEvent>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PaymentTimedOutEventHandler> _logger;

    public PaymentTimedOutEventHandler(
        IBookingRepository bookingRepository,
        IRoomRepository roomRepository,
        IUnitOfWork unitOfWork,
        ILogger<PaymentTimedOutEventHandler> logger)
    {
        _bookingRepository = bookingRepository;
        _roomRepository = roomRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(PaymentTimedOutEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Payment window expired for booking {BookingId}. " +
            "Auto-releasing room {RoomNumber}.",
            notification.BookingId, notification.RoomNumber);

        var room = await _roomRepository.GetByIdAsync(notification.RoomId, cancellationToken);
        var booking = await _bookingRepository.GetByIdAsync(notification.BookingId, cancellationToken);

        // Guard: if either was already processed (e.g., payment came in at the same moment
        // as expiry), silently skip — idempotent handler design
        if (room is null || booking is null)
        {
            _logger.LogWarning(
                "PaymentTimedOut for booking {BookingId}: room or booking already cleaned up.",
                notification.BookingId);
            return;
        }

        // Only release if still in the expected state — prevents double-release
        if (room.Status == Domain.Enums.RoomStatus.PendingPayment)
            room.ReleaseFromPaymentHold(); // Raises RoomStatusChangedEvent → SignalR push

        if (booking.Status == Domain.Enums.BookingStatus.Pending)
            booking.Cancel(); // Raises BookingCancelledEvent

        await _roomRepository.UpdateAsync(room, cancellationToken);
        await _bookingRepository.UpdateAsync(booking, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Booking {BookingId} cancelled. Room {RoomNumber} is now Available.",
            notification.BookingId, notification.RoomNumber);
    }
}
