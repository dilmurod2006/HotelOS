using Reception.Domain.Enums;

namespace Reception.Application.DTOs;

public sealed record BookingDto(
    Guid Id,
    Guid GuestId,
    string GuestFullName,
    Guid RoomId,
    string RoomNumber,
    int Floor,
    RoomType RoomType,
    BookingStatus Status,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Nights,
    decimal RoomCharge,
    decimal RoomServiceCharges,
    decimal ExtraFees,
    decimal? TotalBill,
    string Currency,
    DateTime CreatedAt);
