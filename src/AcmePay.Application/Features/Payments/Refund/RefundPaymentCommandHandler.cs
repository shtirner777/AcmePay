using System.Globalization;
using AcmePay.Application.Abstractions.Context;
using AcmePay.Application.Abstractions.Messaging;
using AcmePay.Application.Abstractions.Persistence;
using AcmePay.Application.Abstractions.Time;
using AcmePay.Application.Exceptions;
using AcmePay.Application.Payments.Audit;
using AcmePay.Application.Payments.Idempotency;
using AcmePay.Application.Payments.Repositories;
using AcmePay.Core.Payments.ValueObjects;
using FluentValidation;

namespace AcmePay.Application.Features.Payments.Refund;

public sealed class RefundPaymentCommandHandler(
    IValidator<RefundPaymentCommand> validator,
    IPaymentRepository paymentRepository,
    IIdempotencyStore idempotencyStore,
    IAuditLogWriter auditLogWriter,
    IPaymentAuditLogFactory paymentAuditLogFactory,
    IUnitOfWork unitOfWork,
    IClock clock,
    IExecutionContextAccessor executionContextAccessor)
    : ICommandHandler<RefundPaymentCommand, RefundPaymentResult>
{
    public async Task<RefundPaymentResult> Handle(
        RefundPaymentCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var now = clock.UtcNow;
        var executionContext = executionContextAccessor.GetCurrent();

        var idempotencyRequest = new IdempotencyRequest(
            command.MerchantId,
            PaymentOperations.Refund,
            command.IdempotencyKey,
            IdempotencyRequestHasher.HashParts(
                command.MerchantId.Trim(),
                command.PaymentId.ToString("D"),
                command.Amount.ToString("0.00", CultureInfo.InvariantCulture)),
            now);

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var idempotency = await idempotencyStore.TryBeginAsync<RefundPaymentResult>(
                idempotencyRequest,
                cancellationToken);

            switch (idempotency.State)
            {
                case IdempotencyExecutionState.Completed:
                    await unitOfWork.RollbackAsync(cancellationToken);
                    return idempotency.CachedResponse
                           ?? throw new InvalidOperationException("Cached idempotent response is missing.");

                case IdempotencyExecutionState.InProgress:
                    throw new IdempotencyConflictException(
                        "A request with the same idempotency key is already being processed.");

                case IdempotencyExecutionState.Conflict:
                    throw new IdempotencyConflictException(
                        "The same idempotency key was reused with a different request payload.");

                case IdempotencyExecutionState.Claimed:
                    break;

                default:
                    throw new InvalidOperationException("Unknown idempotency execution state.");
            }

            var payment = await paymentRepository.GetForUpdateAsync(
                new PaymentId(command.PaymentId),
                cancellationToken);

            if (payment is null || payment.MerchantId.Value != command.MerchantId)
            {
                throw new NotFoundException("Payment was not found.");
            }

            var amount = new Money(command.Amount, payment.AuthorizedAmount.Currency);
            var refundReference = new RefundReference(($"REF-{Guid.NewGuid():N}")[..20]);

            payment.Refund(amount, refundReference, now);

            await paymentRepository.UpdateAsync(payment, cancellationToken);

            var auditEntries = paymentAuditLogFactory.Create(payment, executionContext);
            await auditLogWriter.WriteAsync(auditEntries, cancellationToken);

            var result = new RefundPaymentResult(
                payment.Id.Value,
                payment.MerchantId.Value,
                amount.Amount,
                payment.RefundedAmount.Amount,
                payment.RemainingRefundableAmount.Amount,
                payment.AuthorizedAmount.Currency.Code,
                payment.Status.ToString(),
                refundReference.Value,
                now);

            await idempotencyStore.CompleteAsync(idempotencyRequest, result, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            payment.ClearDomainEvents();

            return result;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
