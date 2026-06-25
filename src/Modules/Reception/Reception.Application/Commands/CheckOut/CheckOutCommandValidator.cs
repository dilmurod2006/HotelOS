using FluentValidation;

namespace Reception.Application.Commands.CheckOut;

public sealed class CheckOutCommandValidator : AbstractValidator<CheckOutCommand>
{
    public CheckOutCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty()
            .WithMessage("BookingId is required for check-out.");
    }
}
