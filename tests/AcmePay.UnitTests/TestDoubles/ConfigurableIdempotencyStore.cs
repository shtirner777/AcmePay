using AcmePay.Application.Payments.Idempotency;

namespace AcmePay.UnitTests.TestDoubles;

internal sealed class ConfigurableIdempotencyStore : IIdempotencyStore
{
    public Func<IdempotencyRequest, object?>? CachedResponseFactory { get; set; }
    public IdempotencyExecutionState StateToReturn { get; set; } = IdempotencyExecutionState.Claimed;
    public IdempotencyRequest? LastTryBeginRequest { get; private set; }
    public IdempotencyRequest? LastCompletedRequest { get; private set; }
    public object? LastCompletedResponse { get; private set; }

    public Task<IdempotencyExecutionResult<TResponse>> TryBeginAsync<TResponse>(
        IdempotencyRequest request,
        CancellationToken cancellationToken = default)
    {
        LastTryBeginRequest = request;

        var cached = CachedResponseFactory?.Invoke(request);
        if (cached is TResponse typedCached)
        {
            return Task.FromResult(new IdempotencyExecutionResult<TResponse>(StateToReturn, typedCached));
        }

        return Task.FromResult(new IdempotencyExecutionResult<TResponse>(StateToReturn));
    }

    public Task CompleteAsync<TResponse>(
        IdempotencyRequest request,
        TResponse response,
        CancellationToken cancellationToken = default)
    {
        LastCompletedRequest = request;
        LastCompletedResponse = response;
        return Task.CompletedTask;
    }
}
