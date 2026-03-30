using System.Text.Json;
using AppExecutionContext = AcmePay.Application.Abstractions.Context.ExecutionContext;
using AcmePay.Application.Payments.Audit;
using AcmePay.Core.Payments.Enums;
using AcmePay.Core.Payments.ValueObjects;
using Xunit;

namespace AcmePay.UnitTests.Payments;

public sealed class PaymentAuditLogFactoryTests
{
    [Fact]
    public void Create_ShouldMapRefundAfterPartialCapture_WithCorrectOldStateAndTimestamps()
    {
        var authorizedAt = new DateTimeOffset(2026, 3, 30, 10, 0, 0, TimeSpan.Zero);
        var capturedAt = authorizedAt.AddMinutes(10);
        var refundedAt = authorizedAt.AddMinutes(20);

        var payment = PaymentTestData.CreateAuthorizedPayment(100m, authorizedAt);

        payment.ClearDomainEvents();
        payment.Capture(new Money(40m, new Currency(PaymentTestConstants.CurrencyCode)), new CaptureReference("CAP-000000000000001"), capturedAt);
        payment.ClearDomainEvents();
        payment.Refund(new Money(10m, new Currency(PaymentTestConstants.CurrencyCode)), new RefundReference("REF-000000000000001"), refundedAt);

        var factory = new PaymentAuditLogFactory();
        var executionContext = new AppExecutionContext(PaymentTestConstants.TriggeredBy, PaymentTestConstants.CorrelationId);

        var entry = Assert.Single(factory.Create(payment, executionContext));

        Assert.Equal(PaymentTestConstants.TriggeredBy, entry.TriggeredBy);
        Assert.Equal(PaymentTestConstants.CorrelationId, entry.CorrelationId);
        Assert.Equal(refundedAt, entry.OccurredOnUtc);
        Assert.Equal(PaymentStatus.PartiallyCaptured.ToString(), entry.OldState);
        Assert.Equal(PaymentStatus.PartiallyRefunded.ToString(), entry.NewState);

        using var json = JsonDocument.Parse(entry.PayloadJson!);
        Assert.Equal(10m, json.RootElement.GetProperty("amount").GetDecimal());
        Assert.Equal(30m, json.RootElement.GetProperty("remainingRefundableAmount").GetDecimal());
    }
}
