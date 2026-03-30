using System.Text.Json;
using AcmePay.Application.Abstractions.Context;
using AcmePay.Core.Abstractions;
using AcmePay.Core.Payments.Aggregates;
using AcmePay.Core.Payments.DomainEvents;
using AcmePay.Core.Payments.Enums;
using ExecutionContext = AcmePay.Application.Abstractions.Context.ExecutionContext;

namespace AcmePay.Application.Payments.Audit;

public sealed class PaymentAuditLogFactory : IPaymentAuditLogFactory
{
    private const string PaymentAggregateType = "Payment";

    public IReadOnlyCollection<AuditLogEntry> Create(
        Payment payment,
        ExecutionContext executionContext)
    {
        return payment.DomainEvents
            .Select(domainEvent => MapAuditEntry(payment, domainEvent, executionContext))
            .ToArray();
    }

    private static AuditLogEntry MapAuditEntry(
        Payment payment,
        DomainEvent domainEvent,
        ExecutionContext executionContext)
    {
        return domainEvent switch
        {
            PaymentAuthorizedDomainEvent authorized => new AuditLogEntry(
                AggregateType: PaymentAggregateType,
                AggregateId: payment.Id.Value.ToString(),
                EventType: nameof(PaymentAuthorizedDomainEvent),
                TriggeredBy: executionContext.TriggeredBy,
                CorrelationId: executionContext.CorrelationId,
                OccurredOnUtc: authorized.OccurredOnUtc,
                OldState: null,
                NewState: payment.Status.ToString(),
                PayloadJson: JsonSerializer.Serialize(new
                {
                    paymentId = authorized.PaymentId.Value,
                    merchantId = authorized.MerchantId.Value,
                    amount = authorized.Amount.Amount,
                    currency = authorized.Amount.Currency.Code,
                    authorizationReference = authorized.AuthorizationReference.Value
                })),

            PaymentCapturedDomainEvent captured => new AuditLogEntry(
                AggregateType: PaymentAggregateType,
                AggregateId: payment.Id.Value.ToString(),
                EventType: nameof(PaymentCapturedDomainEvent),
                TriggeredBy: executionContext.TriggeredBy,
                CorrelationId: executionContext.CorrelationId,
                OccurredOnUtc: captured.OccurredOnUtc,
                OldState: GetCaptureOldState(payment, captured).ToString(),
                NewState: payment.Status.ToString(),
                PayloadJson: JsonSerializer.Serialize(new
                {
                    paymentId = captured.PaymentId.Value,
                    amount = captured.Amount.Amount,
                    currency = captured.Amount.Currency.Code,
                    captureReference = captured.CaptureReference.Value,
                    totalCapturedAmount = payment.CapturedAmount.Amount,
                    remainingAuthorizedAmount = payment.RemainingAuthorizedAmount.Amount
                })),

            PaymentVoidedDomainEvent voided => new AuditLogEntry(
                AggregateType: PaymentAggregateType,
                AggregateId: payment.Id.Value.ToString(),
                EventType: nameof(PaymentVoidedDomainEvent),
                TriggeredBy: executionContext.TriggeredBy,
                CorrelationId: executionContext.CorrelationId,
                OccurredOnUtc: voided.OccurredOnUtc,
                OldState: PaymentStatus.Authorized.ToString(),
                NewState: payment.Status.ToString(),
                PayloadJson: JsonSerializer.Serialize(new
                {
                    paymentId = voided.PaymentId.Value
                })),

            PaymentRefundedDomainEvent refunded => new AuditLogEntry(
                AggregateType: PaymentAggregateType,
                AggregateId: payment.Id.Value.ToString(),
                EventType: nameof(PaymentRefundedDomainEvent),
                TriggeredBy: executionContext.TriggeredBy,
                CorrelationId: executionContext.CorrelationId,
                OccurredOnUtc: refunded.OccurredOnUtc,
                OldState: GetRefundOldState(payment, refunded).ToString(),
                NewState: payment.Status.ToString(),
                PayloadJson: JsonSerializer.Serialize(new
                {
                    paymentId = refunded.PaymentId.Value,
                    amount = refunded.Amount.Amount,
                    currency = refunded.Amount.Currency.Code,
                    refundReference = refunded.RefundReference.Value,
                    totalRefundedAmount = payment.RefundedAmount.Amount,
                    remainingRefundableAmount = payment.RemainingRefundableAmount.Amount
                })),

            _ => throw new InvalidOperationException(
                $"Unsupported domain event type for audit mapping: {domainEvent.GetType().Name}")
        };
    }

    private static PaymentStatus GetCaptureOldState(Payment payment, PaymentCapturedDomainEvent domainEvent)
    {
        var wasFirstCapture = payment.CapturedAmount == domainEvent.Amount;
        return wasFirstCapture ? PaymentStatus.Authorized : PaymentStatus.PartiallyCaptured;
    }

    private static PaymentStatus GetRefundOldState(Payment payment, PaymentRefundedDomainEvent domainEvent)
    {
        var wasFirstRefund = payment.RefundedAmount == domainEvent.Amount;
        if (!wasFirstRefund)
        {
            return PaymentStatus.PartiallyRefunded;
        }

        var wasFullyCapturedBeforeRefund = payment.CapturedAmount == payment.AuthorizedAmount;
        return wasFullyCapturedBeforeRefund
            ? PaymentStatus.Captured
            : PaymentStatus.PartiallyCaptured;
    }
}
