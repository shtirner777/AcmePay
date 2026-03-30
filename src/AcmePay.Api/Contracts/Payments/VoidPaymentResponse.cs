namespace AcmePay.Api.Contracts.Payments;

public sealed record VoidPaymentResponse(
    Guid PaymentId,
    string MerchantId,
    string Status,
    DateTimeOffset VoidedAtUtc);
