using MediatR;
using Microsoft.Extensions.Logging;
using HotelOS.SharedKernel.Common;
using Reception.Application.DTOs;
using Reception.Application.Interfaces;
using Reception.Domain.Enums;
using Reception.Domain.ValueObjects;

namespace Reception.Application.Commands.CheckOut;

/// <summary>
/// Handler: CheckOutCommandHandler.
///
/// ══════════════════════════════════════════════════════════════════════════════
/// BILLING ALGORITHM (BTEC Task 1.2 — required algorithm)
/// ══════════════════════════════════════════════════════════════════════════════
///
/// Formula:
///   Total Bill = (NightlyRate × NightCount)    ← Room Charge
///              + RoomServiceCharges             ← Accumulated during stay
///              + ExtraFees                      ← Minibar, late checkout, etc.
///
/// The algorithm handles these edge cases:
///   • Zero room service charges (guest ordered nothing) → no crash, adds £0
///   • Late checkout fee: optional £50 applied only if ApplyLateCheckoutFee = true
///   • Early checkout (fewer nights than booked): NightCount from actual DateRange
///     is already computed at booking-creation time; if the guest checks out early,
///     the system creates a new DateRange reflecting actual nights stayed.
///     (For BTEC scope: we use the original booked nights and note this as a
///      future enhancement point.)
///
/// ══════════════════════════════════════════════════════════════════════════════
/// EVENT-DRIVEN CHAIN (BTEC Task 2.4 — event-driven programming demonstration)
/// ══════════════════════════════════════════════════════════════════════════════
///
/// When room.CheckOut() is called, it internally raises:
///   1. RoomVacatedEvent  → Housekeeping module subscribes → creates CleaningTask
///   2. RoomStatusChangedEvent → SignalR hub pushes "Room 204 is now Dirty" to dashboard
///
/// The handler does NOT call Housekeeping directly. MediatR dispatches these events
/// after SaveChangesAsync, maintaining strict module isolation.
/// </summary>
public sealed class CheckOutCommandHandler
    : IRequestHandler<CheckOutCommand, Result<BillDto>>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IGuestRepository _guestRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CheckOutCommandHandler> _logger;

    // Business constant: late checkout fee amount and currency
    // Named constant enforces coding standard — no magic numbers in logic
    private static readonly Money LateCheckoutFee = Money.Of(50m, "USD");
    private const string LateCheckoutDescription = "Late check-out fee (after 12:00)";

    public CheckOutCommandHandler(
        IBookingRepository bookingRepository,
        IRoomRepository roomRepository,
        IGuestRepository guestRepository,
        IUnitOfWork unitOfWork,
        ILogger<CheckOutCommandHandler> logger)
    {
        _bookingRepository = bookingRepository;
        _roomRepository = roomRepository;
        _guestRepository = guestRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<BillDto>> Handle(
        CheckOutCommand command,
        CancellationToken cancellationToken)
    {
        // ── LOAD BOOKING & VALIDATE STATE ────────────────────────────────────
        var booking = await _bookingRepository.GetByIdAsync(command.BookingId, cancellationToken);
        if (booking is null)
            return Result<BillDto>.Failure(
                new Error("CheckOut.BookingNotFound", $"Booking {command.BookingId} not found."));

        if (booking.Status != BookingStatus.CheckedIn)
            return Result<BillDto>.Failure(
                new Error("CheckOut.InvalidStatus",
                    $"Cannot check out — booking status is {booking.Status}, expected CheckedIn."));

        var room = await _roomRepository.GetByIdAsync(booking.RoomId, cancellationToken);
        if (room is null)
            return Result<BillDto>.Failure(Error.NotFound);

        var guest = await _guestRepository.GetByIdAsync(booking.GuestId, cancellationToken);
        if (guest is null)
            return Result<BillDto>.Failure(Error.NotFound);

        // ── BILLING ALGORITHM ─────────────────────────────────────────────────
        // Step 1: Apply optional late check-out surcharge BEFORE calling CheckOut()
        //         because CheckOut() seals the bill — no modifications allowed after.
        if (command.ApplyLateCheckoutFee)
        {
            // Booking.AddExtraFee() enforces the "only while CheckedIn" invariant.
            // The fee is itemised in ExtraFeeLineItems for audit transparency.
            booking.AddExtraFee(LateCheckoutDescription, LateCheckoutFee);

            _logger.LogInformation(
                "Late check-out fee {Amount} applied to booking {BookingId}",
                LateCheckoutFee, command.BookingId);
        }

        // Step 2: Compute final bill.
        //   Booking.CheckOut() executes:
        //     TotalBill = RoomCharge + RoomServiceCharges + ExtraFees
        //   All three Money values were accumulated throughout the stay.
        //   The result is an immutable Money record — no rounding ambiguity.
        var totalBill = booking.CheckOut();

        // Step 3: Transition room status → Dirty.
        //   room.CheckOut() internally raises:
        //     • RoomVacatedEvent     (→ Housekeeping module)
        //     • RoomStatusChangedEvent (→ SignalR dashboard)
        //   These events are collected by AggregateRoot and dispatched by UoW
        //   AFTER the database transaction commits — guaranteed delivery on success.
        room.CheckOut();

        // ── PERSIST ──────────────────────────────────────────────────────────
        await _bookingRepository.UpdateAsync(booking, cancellationToken);
        await _roomRepository.UpdateAsync(room, cancellationToken);

        // SaveChangesAsync commits the transaction THEN dispatches all collected
        // domain events via MediatR.Publish() — Housekeeping handler fires here.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Guest {GuestName} checked out of room {RoomNumber}. " +
            "Total bill: {Currency} {Amount:F2}. Room is now Dirty.",
            guest.Name.FullName, room.RoomNumber, totalBill.Currency, totalBill.Amount);

        // ── BUILD ITEMISED BILL DTO ───────────────────────────────────────────
        // BillDto is the final receipt handed to the receptionist.
        // Note: RoomService charges and ExtraFees are surfaced separately for
        // transparency — the guest can see exactly what each line represents.
        return Result<BillDto>.Success(new BillDto(
            BookingId: booking.Id,
            GuestFullName: guest.Name.FullName,
            RoomNumber: room.RoomNumber.Value,
            CheckIn: booking.StayPeriod.CheckIn,
            CheckOut: booking.StayPeriod.CheckOut,
            Nights: booking.StayPeriod.NightCount,
            NightlyRate: room.NightlyRate.Amount,
            RoomCharge: booking.RoomCharge.Amount,
            RoomServiceCharges: booking.RoomServiceCharges.Amount,
            ExtraFees: booking.ExtraFees.Amount,
            TotalBill: totalBill.Amount,
            Currency: totalBill.Currency,
            CheckOutAt: DateTime.UtcNow));
    }
}
