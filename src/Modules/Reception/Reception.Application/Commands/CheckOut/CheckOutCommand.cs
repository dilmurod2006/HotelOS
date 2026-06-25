using MediatR;
using HotelOS.SharedKernel.Common;
using Reception.Application.DTOs;

namespace Reception.Application.Commands.CheckOut;

/// <summary>
/// Command: Process a guest check-out.
/// Triggers the Billing Algorithm and publishes RoomVacatedEvent to Housekeeping.
/// </summary>
public sealed record CheckOutCommand(
    Guid BookingId,
    bool ApplyLateCheckoutFee = false   // True if check-out is after the hotel's 12:00 cutoff
) : IRequest<Result<BillDto>>;
