using AcmePay.Application.Payments.Idempotency;

namespace AcmePay.IntegrationTests.TestHost;

internal sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly object _sync = new();
    private readonly Dictionary<(string MerchantId, string Operation, string IdempotencyKey), Entry> _entries = new();

    public Task<IdempotencyExecutionResult<TResponse>> TryBeginAsync<TResponse>(
        IdempotencyRequest request,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var key = (request.MerchantId, request.Operation, request.IdempotencyKey);
            if (!_entries.TryGetValue(key, out var existing))
            {
                _entries[key] = new Entry(request.RequestHash, IdempotencyExecutionState.Claimed, null, request.RequestedAtUtc, null);
                return Task.FromResult(new IdempotencyExecutionResult<TResponse>(IdempotencyExecutionState.Claimed));
            }

            if (!string.Equals(existing.RequestHash, request.RequestHash, StringComparison.Ordinal))
            {
                return Task.FromResult(new IdempotencyExecutionResult<TResponse>(IdempotencyExecutionState.Conflict));
            }

            if (existing.State == IdempotencyExecutionState.Completed)
            {
                return Task.FromResult(new IdempotencyExecutionResult<TResponse>(IdempotencyExecutionState.Completed, (TResponse?)existing.Response));
            }

            return Task.FromResult(new IdempotencyExecutionResult<TResponse>(IdempotencyExecutionState.InProgress));
        }
    }

    public Task CompleteAsync<TResponse>(
        IdempotencyRequest request,
        TResponse response,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var key = (request.MerchantId, request.Operation, request.IdempotencyKey);
            if (!_entries.TryGetValue(key, out var existing))
            {
                throw new InvalidOperationException("Idempotency entry was not claimed before completion.");
            }

            _entries[key] = existing with
            {
                State = IdempotencyExecutionState.Completed,
                Response = response
            };
        }

        return Task.CompletedTask;
    }

    private sealed record Entry(
        string RequestHash,
        IdempotencyExecutionState State,
        object? Response,
        DateTimeOffset RequestedAtUtc,
        DateTimeOffset? CompletedAtUtc);
}
