using Dapper;
using AcmePay.Application.Abstractions.Persistence;
using AcmePay.Application.Payments.Audit;

namespace AcmePay.Infrastructure.Audit;

public sealed class AuditLogWriter(IUnitOfWork unitOfWork) : IAuditLogWriter
{
    public async Task WriteAsync(
        IReadOnlyCollection<AuditLogEntry> entries,
        CancellationToken cancellationToken = default)
    {
        if (entries.Count == 0)
        {
            return;
        }

        const string sql = """
                           insert into payment_audit_log
                           (
                               aggregate_type,
                               aggregate_id,
                               event_type,
                               triggered_by,
                               correlation_id,
                               occurred_on_utc,
                               old_state,
                               new_state,
                               payload_json
                           )
                           values
                           (
                               @AggregateType,
                               @AggregateId,
                               @EventType,
                               @TriggeredBy,
                               @CorrelationId,
                               @OccurredOnUtc,
                               @OldState,
                               @NewState,
                               cast(@PayloadJson as jsonb)
                           );
                           """;

        var command = new CommandDefinition(
            sql,
            entries.Select(x => new
            {
                x.AggregateType,
                x.AggregateId,
                x.EventType,
                x.TriggeredBy,
                x.CorrelationId,
                x.OccurredOnUtc,
                x.OldState,
                x.NewState,
                x.PayloadJson
            }),
            transaction: unitOfWork.Transaction,
            cancellationToken: cancellationToken);

        await unitOfWork.Connection.ExecuteAsync(command);
    }
}