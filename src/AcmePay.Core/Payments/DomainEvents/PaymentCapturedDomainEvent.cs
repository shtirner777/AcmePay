using AcmePay.Core.Abstractions;
using AcmePay.Core.Payments.ValueObjects;

namespace AcmePay.Core.Payments.DomainEvents;

public sealed record PaymentCapturedDomainEvent(
    PaymentId PaymentId,
    Money Amount,
    CaptureReference CaptureReference,
    DateTimeOffset OccurredOnUtc) : DomainEvent(OccurredOnUtc);