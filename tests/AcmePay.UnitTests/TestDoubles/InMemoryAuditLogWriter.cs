using AcmePay.Application.Payments.Audit;

namespace AcmePay.UnitTests.TestDoubles;

internal sealed class InMemoryAuditLogWriter : IAuditLogWriter
{
    public List<AuditLogEntry> Entries { get; } = new();

    public Task WriteAsync(IReadOnlyCollection<AuditLogEntry> entries, CancellationToken cancellationToken = default)
    {
        Entries.AddRange(entries);
        return Task.CompletedTask;
    }
}
