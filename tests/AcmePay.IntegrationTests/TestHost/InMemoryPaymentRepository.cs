using AcmePay.Application.Payments.Repositories;
using AcmePay.Core.Payments.Aggregates;
using AcmePay.Core.Payments.ValueObjects;

namespace AcmePay.IntegrationTests.TestHost;

internal sealed class InMemoryPaymentRepository : IPaymentRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, Payment> _payments = new();

    public int Count
    {
        get { lock (_sync) return _payments.Count; }
    }

    public IReadOnlyCollection<Payment> Snapshot()
    {
        lock (_sync)
        {
            return _payments.Values.ToArray();
        }
    }

    public Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _payments[payment.Id.Value] = payment;
        }

        return Task.CompletedTask;
    }

    public Task<Payment?> GetByIdAsync(PaymentId paymentId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _payments.TryGetValue(paymentId.Value, out var payment);
            return Task.FromResult(payment);
        }
    }

    public Task<Payment?> GetForUpdateAsync(PaymentId paymentId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _payments.TryGetValue(paymentId.Value, out var payment);
            return Task.FromResult(payment);
        }
    }

    public Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _payments[payment.Id.Value] = payment;
        }

        return Task.CompletedTask;
    }
}
