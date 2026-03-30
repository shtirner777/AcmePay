using FluentValidation;

namespace AcmePay.Application.Features.Payments.Void;

public sealed class VoidPaymentCommandValidator : AbstractValidator<VoidPaymentCommand>
{
    public VoidPaymentCommandValidator()
    {
        RuleFor(x => x.MerchantId)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.PaymentId)
            .NotEmpty();

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(200);
    }
}
