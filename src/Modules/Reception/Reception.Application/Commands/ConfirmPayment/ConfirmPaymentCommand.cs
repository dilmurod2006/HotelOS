using MediatR;
using HotelOS.SharedKernel.Common;
using Reception.Application.DTOs;

namespace Reception.Application.Commands.ConfirmPayment;

/// <summary>
/// Command: Confirm payment for a booking and formally check the guest in.
/// This is the second step in the two-phase booking flow:
///   Phase 1 → CreateBooking (room held, 15-min TTL starts)
///   Phase 2 → ConfirmPayment (payment verified, room transitions to Occupied)
/// </summary>
public sealed record ConfirmPaymentCommand(
    Guid BookingId,
    string CardLast4Digits   // For receipt masking only — never stored in full
) : IRequest<Result<BookingDto>>;
