using AcmePay.Core.Payments.Aggregates;
using AcmePay.Core.Payments.ValueObjects;

namespace AcmePay.Application.Payments.Repositories;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);

    Task<Payment?> GetByIdAsync(
        PaymentId paymentId,
        CancellationToken cancellationToken = default);

    Task<Payment?> GetForUpdateAsync(
        PaymentId paymentId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default);
}