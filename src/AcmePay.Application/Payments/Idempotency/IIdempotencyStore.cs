namespace AcmePay.Application.Payments.Idempotency;

public interface IIdempotencyStore
{
    Task<IdempotencyExecutionResult<TResponse>> TryBeginAsync<TResponse>(
        IdempotencyRequest request,
        CancellationToken cancellationToken = default);

    Task CompleteAsync<TResponse>(
        IdempotencyRequest request,
        TResponse response,
        CancellationToken cancellationToken = default);
}