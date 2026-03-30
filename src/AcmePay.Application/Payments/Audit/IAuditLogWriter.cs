namespace AcmePay.Application.Payments.Audit;

public interface IAuditLogWriter
{
    Task WriteAsync(
        IReadOnlyCollection<AuditLogEntry> entries,
        CancellationToken cancellationToken = default);
}