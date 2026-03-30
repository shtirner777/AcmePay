using AcmePay.Application.Payments.Repositories;
using AcmePay.Core.Payments.Aggregates;
using AcmePay.Core.Payments.ValueObjects;

namespace AcmePay.UnitTests.TestDoubles;

internal sealed class InMemoryPaymentRepository : IPaymentRepository
{
    private readonly Dictionary<Guid, Payment> _payments = new();

    public IReadOnlyCollection<Payment> Payments => _payments.Values.ToArray();

    public Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _payments[payment.Id.Value] = payment;
        return Task.CompletedTask;
    }

    public Task<Payment?> GetByIdAsync(PaymentId paymentId, CancellationToken cancellationToken = default)
    {
        _payments.TryGetValue(paymentId.Value, out var payment);
        return Task.FromResult(payment);
    }

    public Task<Payment?> GetForUpdateAsync(PaymentId paymentId, CancellationToken cancellationToken = default)
    {
        _payments.TryGetValue(paymentId.Value, out var payment);
        return Task.FromResult(payment);
    }

    public Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _payments[payment.Id.Value] = payment;
        return Task.CompletedTask;
    }
}
