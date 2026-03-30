using FluentValidation;

namespace AcmePay.Application.Features.Payments.Refund;

public sealed class RefundPaymentCommandValidator : AbstractValidator<RefundPaymentCommand>
{
    public RefundPaymentCommandValidator()
    {
        RuleFor(x => x.MerchantId)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.PaymentId)
            .NotEmpty();

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Amount)
            .GreaterThan(0m);
    }
}
