namespace AcmePay.Application.Payments.Idempotency;

public sealed record IdempotencyRequest(
    string MerchantId,
    string Operation,
    string IdempotencyKey,
    string RequestHash,
    DateTimeOffset RequestedAtUtc);