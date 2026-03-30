using AcmePay.Application.Abstractions.Time;
using FluentValidation;

namespace AcmePay.Application.Features.Payments.Authorize;

public sealed class AuthorizePaymentCommandValidator : AbstractValidator<AuthorizePaymentCommand>
{
    public AuthorizePaymentCommandValidator(IClock clock)
    {
        RuleFor(x => x.MerchantId)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Amount)
            .GreaterThan(0m);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .Must(x => x.All(char.IsLetter))
            .WithMessage("Currency must be a 3-letter code.");

        RuleFor(x => x.CardholderName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Pan)
            .NotEmpty()
            .Matches(@"^\d{12,19}$")
            .WithMessage("PAN must contain 12 to 19 digits.");

        RuleFor(x => x.ExpiryMonth)
            .InclusiveBetween(1, 12);

        RuleFor(x => x.ExpiryYear)
            .InclusiveBetween(clock.UtcNow.Year, clock.UtcNow.Year + 20);

        RuleFor(x => x.Cvv)
            .NotEmpty()
            .Matches(@"^\d{3,4}$")
            .WithMessage("CVV must contain 3 or 4 digits.");
    }
}
