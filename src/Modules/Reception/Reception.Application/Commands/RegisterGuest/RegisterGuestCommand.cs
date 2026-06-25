using MediatR;
using HotelOS.SharedKernel.Common;
using Reception.Application.DTOs;

namespace Reception.Application.Commands.RegisterGuest;

/// <summary>
/// Command: Register a new guest or retrieve existing guest by email.
/// Guests must exist before a booking can be created (referential integrity).
/// </summary>
public sealed record RegisterGuestCommand(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber
) : IRequest<Result<GuestDto>>;
