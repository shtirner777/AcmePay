namespace AcmePay.Application.Payments.Idempotency;

public sealed record IdempotencyRequestSnapshot(
    string RequestHash,
    int State,
    string? ResponseJson);
