using AcmePay.Core.Abstractions;
using AcmePay.Core.Payments.ValueObjects;

namespace AcmePay.Core.Payments.DomainEvents;

public sealed record PaymentRefundedDomainEvent(
    PaymentId PaymentId,
    Money Amount,
    RefundReference RefundReference,
    DateTimeOffset OccurredOnUtc) : DomainEvent(OccurredOnUtc);