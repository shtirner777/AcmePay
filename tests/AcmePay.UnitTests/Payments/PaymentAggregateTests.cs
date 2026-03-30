using AcmePay.Core.Exceptions;
using AcmePay.Core.Payments.Aggregates;
using AcmePay.Core.Payments.DomainEvents;
using AcmePay.Core.Payments.Enums;
using AcmePay.Core.Payments.ValueObjects;
using Xunit;

namespace AcmePay.UnitTests.Payments;

public sealed class PaymentAggregateTests
{
    [Fact]
    public void Authorize_ShouldCreateAuthorizedPayment_WithExpectedStateAndTimestamp()
    {
        var authorizedAtUtc = new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero);
        var payment = Payment.Authorize(
            new MerchantId(PaymentTestConstants.MerchantId),
            PaymentTestData.CreatePaymentMethodSnapshot(),
            new Money(100m, new Currency(PaymentTestConstants.CurrencyCode)),
            new AuthorizationReference("AUTH-123456789012"),
            authorizedAtUtc);

        Assert.Equal(PaymentStatus.Authorized, payment.Status);
        Assert.Equal(100m, payment.AuthorizedAmount.Amount);
        Assert.Equal(0m, payment.CapturedAmount.Amount);
        Assert.Equal(0m, payment.RefundedAmount.Amount);
        Assert.Equal(authorizedAtUtc, payment.AuthorizedAtUtc);
        Assert.Equal(authorizedAtUtc, payment.LastModifiedAtUtc);

        var domainEvent = Assert.IsType<PaymentAuthorizedDomainEvent>(Assert.Single(payment.DomainEvents));
        Assert.Equal(authorizedAtUtc, domainEvent.OccurredOnUtc);
    }

    [Fact]
    public void Capture_ShouldTransitionToPartialThenFullCapture_WithConsistentTimes()
    {
        var payment = PaymentTestData.CreateAuthorizedPayment(100m);
        var firstCaptureAt = new DateTimeOffset(2026, 3, 30, 12, 10, 0, TimeSpan.Zero);
        var secondCaptureAt = new DateTimeOffset(2026, 3, 30, 12, 20, 0, TimeSpan.Zero);

        payment.Capture(
            new Money(40m, new Currency(PaymentTestConstants.CurrencyCode)),
            new CaptureReference("CAP-000000000000001"),
            firstCaptureAt);

        Assert.Equal(PaymentStatus.PartiallyCaptured, payment.Status);
        Assert.Equal(40m, payment.CapturedAmount.Amount);
        Assert.Equal(firstCaptureAt, payment.LastModifiedAtUtc);

        payment.Capture(
            new Money(60m, new Currency(PaymentTestConstants.CurrencyCode)),
            new CaptureReference("CAP-000000000000002"),
            secondCaptureAt);

        Assert.Equal(PaymentStatus.Captured, payment.Status);
        Assert.Equal(100m, payment.CapturedAmount.Amount);
        Assert.Equal(0m, payment.RemainingAuthorizedAmount.Amount);
        Assert.Equal(secondCaptureAt, payment.LastModifiedAtUtc);
        Assert.Equal(2, payment.DomainEvents.OfType<PaymentCapturedDomainEvent>().Count());
    }

    [Fact]
    public void Capture_AfterVoid_ShouldThrowDomainRuleViolation()
    {
        var payment = PaymentTestData.CreateAuthorizedPayment(100m);
        payment.Void(new DateTimeOffset(2026, 3, 30, 12, 5, 0, TimeSpan.Zero));

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            payment.Capture(
                new Money(10m, new Currency(PaymentTestConstants.CurrencyCode)),
                new CaptureReference("CAP-000000000000001"),
                new DateTimeOffset(2026, 3, 30, 12, 6, 0, TimeSpan.Zero)));

        Assert.Contains("capture", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Void_AfterCapture_ShouldThrowDomainRuleViolation()
    {
        var payment = PaymentTestData.CreateAuthorizedPayment(100m);
        payment.Capture(
            new Money(10m, new Currency(PaymentTestConstants.CurrencyCode)),
            new CaptureReference("CAP-000000000000001"),
            new DateTimeOffset(2026, 3, 30, 12, 10, 0, TimeSpan.Zero));

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            payment.Void(new DateTimeOffset(2026, 3, 30, 12, 11, 0, TimeSpan.Zero)));

        Assert.Contains("voided", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Refund_ShouldAllowPartialRefund_AndRejectRefundBeyondCapturedAmount()
    {
        var payment = PaymentTestData.CreateAuthorizedPayment(100m);
        payment.Capture(
            new Money(100m, new Currency(PaymentTestConstants.CurrencyCode)),
            new CaptureReference("CAP-000000000000001"),
            new DateTimeOffset(2026, 3, 30, 12, 10, 0, TimeSpan.Zero));

        var refundedAtUtc = new DateTimeOffset(2026, 3, 30, 12, 20, 0, TimeSpan.Zero);
        payment.Refund(
            new Money(30m, new Currency(PaymentTestConstants.CurrencyCode)),
            new RefundReference("REF-000000000000001"),
            refundedAtUtc);

        Assert.Equal(PaymentStatus.PartiallyRefunded, payment.Status);
        Assert.Equal(30m, payment.RefundedAmount.Amount);
        Assert.Equal(70m, payment.RemainingRefundableAmount.Amount);
        Assert.Equal(refundedAtUtc, payment.LastModifiedAtUtc);

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            payment.Refund(
                new Money(71m, new Currency(PaymentTestConstants.CurrencyCode)),
                new RefundReference("REF-000000000000002"),
                refundedAtUtc.AddMinutes(1)));

        Assert.Contains("refund", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Refund_BeforeCapture_ShouldThrowDomainRuleViolation()
    {
        var payment = PaymentTestData.CreateAuthorizedPayment(100m);

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            payment.Refund(
                new Money(10m, new Currency(PaymentTestConstants.CurrencyCode)),
                new RefundReference("REF-000000000000001"),
                new DateTimeOffset(2026, 3, 30, 12, 10, 0, TimeSpan.Zero)));

        Assert.Contains("refund", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Refund_FullAmount_ShouldTransitionToRefunded()
    {
        var payment = PaymentTestData.CreateAuthorizedPayment(100m);
        payment.Capture(
            new Money(100m, new Currency(PaymentTestConstants.CurrencyCode)),
            new CaptureReference("CAP-000000000000001"),
            new DateTimeOffset(2026, 3, 30, 12, 10, 0, TimeSpan.Zero));

        payment.Refund(
            new Money(100m, new Currency(PaymentTestConstants.CurrencyCode)),
            new RefundReference("REF-000000000000001"),
            new DateTimeOffset(2026, 3, 30, 12, 20, 0, TimeSpan.Zero));

        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.Equal(0m, payment.RemainingRefundableAmount.Amount);
    }
}
