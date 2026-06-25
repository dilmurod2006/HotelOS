using FluentValidation;

namespace Reception.Application.Commands.CreateBooking;

/// <summary>
/// FluentValidation validator for CreateBookingCommand.
///
/// ISO/IEC 27001 — all data entering the system at API boundaries must be
/// validated before processing. This validator runs via the MediatR pipeline
/// behaviour (ValidationBehaviour) before the handler is ever invoked.
/// Validation errors are returned as structured API responses, never as
/// raw exceptions that could leak internal details.
/// </summary>
public sealed class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.GuestId)
            .NotEmpty()
            .WithMessage("GuestId is required.");

        RuleFor(x => x.CheckIn)
            // Prevent bookings for dates already in the past
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.Date))
            .WithMessage("Check-in date cannot be in the past.");

        RuleFor(x => x.CheckOut)
            // Check-out must be strictly after check-in — no same-day bookings
            .GreaterThan(x => x.CheckIn)
            .WithMessage("Check-out date must be after check-in date.");

        RuleFor(x => x)
            // Maximum stay enforced by business policy: 30 nights
            .Must(x => (x.CheckOut.DayNumber - x.CheckIn.DayNumber) <= 30)
            .WithMessage("Maximum stay duration is 30 nights.")
            .OverridePropertyName("StayDuration");

        RuleFor(x => x.PreferredFloor)
            // Floor 1–6 only; null means no preference (valid)
            .InclusiveBetween(1, 6)
            .When(x => x.PreferredFloor.HasValue)
            .WithMessage("Preferred floor must be between 1 and 6.");

        RuleFor(x => x.RequestedRoomType)
            .IsInEnum()
            .WithMessage("Invalid room type specified.");

        RuleFor(x => x.ProximityPreference)
            .IsInEnum()
            .WithMessage("Invalid proximity preference specified.");
    }
}
