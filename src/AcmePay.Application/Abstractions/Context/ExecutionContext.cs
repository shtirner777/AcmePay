namespace AcmePay.Application.Abstractions.Context;

public sealed record ExecutionContext(
    string TriggeredBy,
    string? CorrelationId);