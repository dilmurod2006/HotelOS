using MediatR;
using Microsoft.Extensions.Logging;
using HotelOS.SharedKernel.Common;
using Reception.Application.DTOs;
using Reception.Application.Interfaces;
using Reception.Domain.Entities;

namespace Reception.Application.Commands.RegisterGuest;

/// <summary>
/// Handler: RegisterGuestCommandHandler.
/// Idempotent: if a guest with the same email already exists, returns their existing record
/// rather than throwing a duplicate error. Reception desks commonly re-register returning guests.
/// </summary>
public sealed class RegisterGuestCommandHandler
    : IRequestHandler<RegisterGuestCommand, Result<GuestDto>>
{
    private readonly IGuestRepository _guestRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RegisterGuestCommandHandler> _logger;

    public RegisterGuestCommandHandler(
        IGuestRepository guestRepository,
        IUnitOfWork unitOfWork,
        ILogger<RegisterGuestCommandHandler> logger)
    {
        _guestRepository = guestRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<GuestDto>> Handle(
        RegisterGuestCommand command,
        CancellationToken cancellationToken)
    {
        // Idempotency check: prevent duplicate guest records for the same email
        var existing = await _guestRepository.GetByEmailAsync(
            command.Email.Trim().ToLowerInvariant(),
            cancellationToken);

        if (existing is not null)
        {
            _logger.LogInformation("Returning existing guest record for {Email}", command.Email);
            return Result<GuestDto>.Success(MapToDto(existing));
        }

        // GuestName and email validation enforced by the domain Value Objects
        var guest = Guest.Create(
            command.FirstName,
            command.LastName,
            command.Email,
            command.PhoneNumber);

        await _guestRepository.AddAsync(guest, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Registered new guest {GuestId} ({Email})", guest.Id, guest.Email);

        return Result<GuestDto>.Success(MapToDto(guest));
    }

    private static GuestDto MapToDto(Guest guest)
        => new(
            Id: guest.Id,
            FullName: guest.Name.FullName,
            Email: guest.Email,
            PhoneNumber: guest.PhoneNumber,
            MaskedCard: guest.MaskedCardLast4);
}
