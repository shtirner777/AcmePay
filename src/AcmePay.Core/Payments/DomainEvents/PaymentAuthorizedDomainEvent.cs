using AcmePay.Core.Abstractions;
using AcmePay.Core.Payments.ValueObjects;

namespace AcmePay.Core.Payments.DomainEvents;

public sealed record PaymentAuthorizedDomainEvent(
    PaymentId PaymentId,
    MerchantId MerchantId,
    Money Amount,
    AuthorizationReference AuthorizationReference,
    DateTimeOffset OccurredOnUtc) : DomainEvent(OccurredOnUtc);