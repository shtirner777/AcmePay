using System.Text.Json;
using Dapper;
using AcmePay.Application.Abstractions.Persistence;
using AcmePay.Application.Abstractions.Time;
using AcmePay.Application.Payments.Idempotency;

namespace AcmePay.Infrastructure.Idempotency;

public sealed class IdempotencyStore(IUnitOfWork unitOfWork, IClock clock) : IIdempotencyStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IdempotencyExecutionResult<TResponse>> TryBeginAsync<TResponse>(
        IdempotencyRequest request,
        CancellationToken cancellationToken = default)
    {
        const string insertSql = """
            insert into idempotency_requests
            (
                merchant_id,
                operation,
                idempotency_key,
                request_hash,
                state,
                requested_at_utc
            )
            values
            (
                @MerchantId,
                @Operation,
                @IdempotencyKey,
                @RequestHash,
                @State,
                @RequestedAtUtc
            )
            on conflict (merchant_id, operation, idempotency_key) do nothing;
            """;

        const string selectSql = """
            select
                request_hash as RequestHash,
                state as State,
                response_json as ResponseJson
            from idempotency_requests
            where merchant_id = @MerchantId
              and operation = @Operation
              and idempotency_key = @IdempotencyKey;
            """;

        var insertCommand = new CommandDefinition(
            insertSql,
            new
            {
                request.MerchantId,
                request.Operation,
                request.IdempotencyKey,
                request.RequestHash,
                State = (int)IdempotencyExecutionState.Claimed,
                RequestedAtUtc = request.RequestedAtUtc
            },
            transaction: unitOfWork.Transaction,
            cancellationToken: cancellationToken);

        var affected = await unitOfWork.Connection.ExecuteAsync(insertCommand);
        if (affected == 1)
        {
            return new IdempotencyExecutionResult<TResponse>(IdempotencyExecutionState.Claimed);
        }

        var selectCommand = new CommandDefinition(
            selectSql,
            new
            {
                request.MerchantId,
                request.Operation,
                request.IdempotencyKey
            },
            transaction: unitOfWork.Transaction,
            cancellationToken: cancellationToken);

        var snapshot = await unitOfWork.Connection.QuerySingleAsync<IdempotencyRequestSnapshot>(selectCommand);

        if (!string.Equals(snapshot.RequestHash, request.RequestHash, StringComparison.Ordinal))
        {
            return new IdempotencyExecutionResult<TResponse>(IdempotencyExecutionState.Conflict);
        }

        if (snapshot.State == (int)IdempotencyExecutionState.Completed)
        {
            if (string.IsNullOrWhiteSpace(snapshot.ResponseJson))
            {
                throw new InvalidOperationException("Idempotency row is completed but response_json is empty.");
            }

            var cached = JsonSerializer.Deserialize<TResponse>(snapshot.ResponseJson, JsonOptions)
                         ?? throw new InvalidOperationException("Failed to deserialize cached idempotent response.");

            return new IdempotencyExecutionResult<TResponse>(
                IdempotencyExecutionState.Completed,
                cached);
        }

        return new IdempotencyExecutionResult<TResponse>(IdempotencyExecutionState.InProgress);
    }

    public async Task CompleteAsync<TResponse>(
        IdempotencyRequest request,
        TResponse response,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            update idempotency_requests
            set
                state = @State,
                response_json = cast(@ResponseJson as jsonb),
                completed_at_utc = @CompletedAtUtc
            where merchant_id = @MerchantId
              and operation = @Operation
              and idempotency_key = @IdempotencyKey
              and request_hash = @RequestHash;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                request.MerchantId,
                request.Operation,
                request.IdempotencyKey,
                request.RequestHash,
                State = (int)IdempotencyExecutionState.Completed,
                ResponseJson = JsonSerializer.Serialize(response, JsonOptions),
                CompletedAtUtc = clock.UtcNow
            },
            transaction: unitOfWork.Transaction,
            cancellationToken: cancellationToken);

        var affected = await unitOfWork.Connection.ExecuteAsync(command);
        if (affected != 1)
        {
            throw new InvalidOperationException("Failed to complete idempotency record.");
        }
    }
}
