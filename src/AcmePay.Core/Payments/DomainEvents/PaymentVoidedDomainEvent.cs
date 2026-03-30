using AcmePay.Core.Abstractions;
using AcmePay.Core.Payments.ValueObjects;

namespace AcmePay.Core.Payments.DomainEvents;

public sealed record PaymentVoidedDomainEvent(
    PaymentId PaymentId,
    DateTimeOffset OccurredOnUtc) : DomainEvent(OccurredOnUtc);