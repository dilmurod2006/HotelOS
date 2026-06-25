namespace Reception.Application.DTOs;

public sealed record GuestDto(
    Guid Id,
    string FullName,
    string Email,
    string PhoneNumber,
    string? MaskedCard);
