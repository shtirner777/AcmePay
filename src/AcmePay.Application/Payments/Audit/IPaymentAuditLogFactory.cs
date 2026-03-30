using AcmePay.Application.Abstractions.Context;
using AcmePay.Core.Payments.Aggregates;
using ExecutionContext = AcmePay.Application.Abstractions.Context.ExecutionContext;

namespace AcmePay.Application.Payments.Audit;

public interface IPaymentAuditLogFactory
{
    IReadOnlyCollection<AuditLogEntry> Create(
        Payment payment,
        ExecutionContext executionContext);
}
