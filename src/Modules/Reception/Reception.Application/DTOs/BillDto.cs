namespace Reception.Application.DTOs;

/// <summary>
/// Returned by CheckOutCommandHandler. Contains the fully itemised final bill.
/// This is what gets displayed to the receptionist for the guest to settle.
/// </summary>
public sealed record BillDto(
    Guid BookingId,
    string GuestFullName,
    string RoomNumber,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Nights,
    decimal NightlyRate,
    decimal RoomCharge,           // NightlyRate × Nights
    decimal RoomServiceCharges,   // Sum of all room service orders
    decimal ExtraFees,            // Minibar, late checkout, etc.
    decimal TotalBill,            // RoomCharge + RoomServiceCharges + ExtraFees
    string Currency,
    DateTime CheckOutAt);
