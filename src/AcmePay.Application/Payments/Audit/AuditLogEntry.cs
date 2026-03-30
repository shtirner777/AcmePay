namespace AcmePay.Application.Payments.Audit;

public sealed record AuditLogEntry(
    string AggregateType,
    string AggregateId,
    string EventType,
    string TriggeredBy,
    string? CorrelationId,
    DateTimeOffset OccurredOnUtc,
    string? OldState,
    string? NewState,
    string? PayloadJson);