namespace AcmePay.Application.Payments.Idempotency;

public sealed record IdempotencyExecutionResult<TResponse>(
    IdempotencyExecutionState State,
    TResponse? CachedResponse = default);