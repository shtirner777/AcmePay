using AcmePay.Application.Payments.Audit;

namespace AcmePay.IntegrationTests.TestHost;

internal sealed class InMemoryAuditLogWriter : IAuditLogWriter
{
    private readonly object _sync = new();
    private readonly List<AuditLogEntry> _entries = new();

    public IReadOnlyCollection<AuditLogEntry> Snapshot()
    {
        lock (_sync)
        {
            return _entries.ToArray();
        }
    }

    public Task WriteAsync(IReadOnlyCollection<AuditLogEntry> entries, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _entries.AddRange(entries);
        }

        return Task.CompletedTask;
    }
}
