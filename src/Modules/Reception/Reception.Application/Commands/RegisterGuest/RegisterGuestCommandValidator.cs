using FluentValidation;

namespace Reception.Application.Commands.RegisterGuest;

public sealed class RegisterGuestCommandValidator : AbstractValidator<RegisterGuestCommand>
{
    public RegisterGuestCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(50).WithMessage("First name cannot exceed 50 characters.")
            // Prevent injection via regex: letters, spaces, hyphens, apostrophes only
            .Matches(@"^[\p{L}\s'\-]+$").WithMessage("First name contains invalid characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters.")
            .Matches(@"^[\p{L}\s'\-]+$").WithMessage("Last name contains invalid characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.")
            .MaximumLength(254).WithMessage("Email cannot exceed 254 characters.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^\+?[\d\s\-\(\)]{7,20}$")
            .WithMessage("Phone number must be 7–20 digits (international format accepted).");
    }
}
