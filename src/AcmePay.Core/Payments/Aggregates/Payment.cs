using AcmePay.Core.Abstractions;
using AcmePay.Core.Exceptions;
using AcmePay.Core.Payments.DomainEvents;
using AcmePay.Core.Payments.Enums;
using AcmePay.Core.Payments.Errors;
using AcmePay.Core.Payments.ValueObjects;

namespace AcmePay.Core.Payments.Aggregates;

public sealed class Payment : AggregateRoot<PaymentId>
{
    private Payment(PaymentId id) : base(id)
    {
    }

    private Payment(
        PaymentId id,
        MerchantId merchantId,
        PaymentMethodSnapshot paymentMethod,
        Money authorizedAmount,
        AuthorizationReference authorizationReference,
        DateTimeOffset authorizedAtUtc)
        : base(id)
    {
        MerchantId = merchantId;
        PaymentMethod = paymentMethod;
        AuthorizedAmount = authorizedAmount;
        CapturedAmount = Money.Zero(authorizedAmount.Currency);
        RefundedAmount = Money.Zero(authorizedAmount.Currency);
        AuthorizationReference = authorizationReference;
        Status = PaymentStatus.Authorized;
        AuthorizedAtUtc = authorizedAtUtc;
        LastModifiedAtUtc = authorizedAtUtc;
    }

    public MerchantId MerchantId { get; private set; }
    public PaymentStatus Status { get; private set; }

    public PaymentMethodSnapshot PaymentMethod { get; private set; } = default!;

    public Money AuthorizedAmount { get; private set; }
    public Money CapturedAmount { get; private set; }
    public Money RefundedAmount { get; private set; }

    public AuthorizationReference AuthorizationReference { get; private set; }

    public DateTimeOffset AuthorizedAtUtc { get; private set; }
    public DateTimeOffset LastModifiedAtUtc { get; private set; }

    public Money RemainingAuthorizedAmount => AuthorizedAmount - CapturedAmount;
    public Money RemainingRefundableAmount => CapturedAmount - RefundedAmount;

    public static Payment Authorize(
        MerchantId merchantId,
        PaymentMethodSnapshot paymentMethod,
        Money amount,
        AuthorizationReference authorizationReference,
        DateTimeOffset authorizedAtUtc)
    {
        if (amount.IsZero)
        {
            throw new DomainRuleViolationException(PaymentErrors.AmountMustBeGreaterThanZero);
        }

        var payment = new Payment(
            PaymentId.New(),
            merchantId,
            paymentMethod,
            amount,
            authorizationReference,
            authorizedAtUtc);

        payment.Raise(new PaymentAuthorizedDomainEvent(
            payment.Id,
            payment.MerchantId,
            payment.AuthorizedAmount,
            payment.AuthorizationReference,
            authorizedAtUtc));

        return payment;
    }

    public static Payment Rehydrate(
        PaymentId id,
        MerchantId merchantId,
        PaymentStatus status,
        PaymentMethodSnapshot paymentMethod,
        Money authorizedAmount,
        Money capturedAmount,
        Money refundedAmount,
        AuthorizationReference authorizationReference,
        DateTimeOffset authorizedAtUtc,
        DateTimeOffset lastModifiedAtUtc)
    {
        return new Payment(id)
        {
            MerchantId = merchantId,
            Status = status,
            PaymentMethod = paymentMethod,
            AuthorizedAmount = authorizedAmount,
            CapturedAmount = capturedAmount,
            RefundedAmount = refundedAmount,
            AuthorizationReference = authorizationReference,
            AuthorizedAtUtc = authorizedAtUtc,
            LastModifiedAtUtc = lastModifiedAtUtc
        };
    }

    public void Capture(
        Money amount,
        CaptureReference captureReference,
        DateTimeOffset capturedAtUtc)
    {
        if (Status is not PaymentStatus.Authorized and not PaymentStatus.PartiallyCaptured)
        {
            throw new DomainRuleViolationException(PaymentErrors.PaymentCannotBeCaptured);
        }

        if (amount.IsZero)
        {
            throw new DomainRuleViolationException(PaymentErrors.AmountMustBeGreaterThanZero);
        }

        if (amount > RemainingAuthorizedAmount)
        {
            throw new DomainRuleViolationException(PaymentErrors.CaptureAmountExceedsRemainingAuthorizedAmount);
        }

        CapturedAmount += amount;
        Status = CapturedAmount == AuthorizedAmount
            ? PaymentStatus.Captured
            : PaymentStatus.PartiallyCaptured;

        LastModifiedAtUtc = capturedAtUtc;

        Raise(new PaymentCapturedDomainEvent(
            Id,
            amount,
            captureReference,
            capturedAtUtc));
    }

    public void Void(DateTimeOffset voidedAtUtc)
    {
        if (Status != PaymentStatus.Authorized || CapturedAmount > Money.Zero(AuthorizedAmount.Currency))
        {
            throw new DomainRuleViolationException(PaymentErrors.PaymentCannotBeVoided);
        }

        Status = PaymentStatus.Voided;
        LastModifiedAtUtc = voidedAtUtc;

        Raise(new PaymentVoidedDomainEvent(
            Id,
            voidedAtUtc));
    }

    public void Refund(
        Money amount,
        RefundReference refundReference,
        DateTimeOffset refundedAtUtc)
    {
        if (CapturedAmount.IsZero || Status == PaymentStatus.Voided || Status == PaymentStatus.Refunded)
        {
            throw new DomainRuleViolationException(PaymentErrors.PaymentCannotBeRefunded);
        }

        if (amount.IsZero)
        {
            throw new DomainRuleViolationException(PaymentErrors.AmountMustBeGreaterThanZero);
        }

        if (amount > RemainingRefundableAmount)
        {
            throw new DomainRuleViolationException(PaymentErrors.RefundAmountExceedsRemainingCapturedAmount);
        }

        RefundedAmount += amount;
        Status = RefundedAmount == CapturedAmount
            ? PaymentStatus.Refunded
            : PaymentStatus.PartiallyRefunded;

        LastModifiedAtUtc = refundedAtUtc;

        Raise(new PaymentRefundedDomainEvent(
            Id,
            amount,
            refundReference,
            refundedAtUtc));
    }
}