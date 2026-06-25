using MediatR;
using Microsoft.Extensions.Logging;
using HotelOS.SharedKernel.Common;
using Reception.Application.DTOs;
using Reception.Application.Interfaces;
using Reception.Domain.Enums;

namespace Reception.Application.Commands.ConfirmPayment;

/// <summary>
/// Handler: ConfirmPaymentCommandHandler.
///
/// Transitions: Booking Pending → Confirmed, Room PendingPayment → Occupied.
/// Deletes the 15-minute Redis TTL key — cancels the auto-release timer since
/// payment was received before the window expired.
/// </summary>
public sealed class ConfirmPaymentCommandHandler
    : IRequestHandler<ConfirmPaymentCommand, Result<BookingDto>>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IGuestRepository _guestRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRedisBookingCache _redisCache;
    private readonly ILogger<ConfirmPaymentCommandHandler> _logger;

    public ConfirmPaymentCommandHandler(
        IBookingRepository bookingRepository,
        IRoomRepository roomRepository,
        IGuestRepository guestRepository,
        IUnitOfWork unitOfWork,
        IRedisBookingCache redisCache,
        ILogger<ConfirmPaymentCommandHandler> logger)
    {
        _bookingRepository = bookingRepository;
        _roomRepository = roomRepository;
        _guestRepository = guestRepository;
        _unitOfWork = unitOfWork;
        _redisCache = redisCache;
        _logger = logger;
    }

    public async Task<Result<BookingDto>> Handle(
        ConfirmPaymentCommand command,
        CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository.GetByIdAsync(command.BookingId, cancellationToken);
        if (booking is null)
            return Result<BookingDto>.Failure(
                new Error("Payment.BookingNotFound", $"Booking {command.BookingId} not found."));

        if (booking.Status != BookingStatus.Pending)
            return Result<BookingDto>.Failure(
                new Error("Payment.AlreadyProcessed",
                    $"Booking {command.BookingId} is not awaiting payment (status: {booking.Status})."));

        var room = await _roomRepository.GetByIdAsync(booking.RoomId, cancellationToken);
        if (room is null)
            return Result<BookingDto>.Failure(Error.NotFound);

        var guest = await _guestRepository.GetByIdAsync(booking.GuestId, cancellationToken);
        if (guest is null)
            return Result<BookingDto>.Failure(Error.NotFound);

        // Record masked card reference on guest profile (PCI DSS — no full PAN stored)
        guest.SetPaymentCard(command.CardLast4Digits);

        // Confirm booking → Pending becomes Confirmed
        booking.ConfirmPayment();

        // Transition room: PendingPayment → Occupied
        // This calls room.CheckIn() which enforces the state transition invariant
        booking.CheckIn();
        room.CheckIn();

        await _bookingRepository.UpdateAsync(booking, cancellationToken);
        await _roomRepository.UpdateAsync(room, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Cancel the 15-minute Redis TTL — payment received, no auto-release needed
        await _redisCache.CancelPaymentWindowAsync(command.BookingId, cancellationToken);

        _logger.LogInformation(
            "Payment confirmed for booking {BookingId}. Room {RoomNumber} is now Occupied.",
            command.BookingId, room.RoomNumber);

        return Result<BookingDto>.Success(new BookingDto(
            Id: booking.Id,
            GuestId: guest.Id,
            GuestFullName: guest.Name.FullName,
            RoomId: room.Id,
            RoomNumber: room.RoomNumber.Value,
            Floor: room.Floor,
            RoomType: room.Type,
            Status: booking.Status,
            CheckIn: booking.StayPeriod.CheckIn,
            CheckOut: booking.StayPeriod.CheckOut,
            Nights: booking.StayPeriod.NightCount,
            RoomCharge: booking.RoomCharge.Amount,
            RoomServiceCharges: booking.RoomServiceCharges.Amount,
            ExtraFees: booking.ExtraFees.Amount,
            TotalBill: booking.TotalBill?.Amount,
            Currency: booking.RoomCharge.Currency,
            CreatedAt: booking.CreatedAt));
    }
}
