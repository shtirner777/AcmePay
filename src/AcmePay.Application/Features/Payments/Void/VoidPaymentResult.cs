namespace AcmePay.Application.Features.Payments.Void;

public sealed record VoidPaymentResult(
    Guid PaymentId,
    string MerchantId,
    string Status,
    DateTimeOffset VoidedAtUtc);
