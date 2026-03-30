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

namespace AcmePay.Application.Features.Payments.Void;

public sealed class VoidPaymentCommandHandler(
    IValidator<VoidPaymentCommand> validator,
    IPaymentRepository paymentRepository,
    IIdempotencyStore idempotencyStore,
    IAuditLogWriter auditLogWriter,
    IPaymentAuditLogFactory paymentAuditLogFactory,
    IUnitOfWork unitOfWork,
    IClock clock,
    IExecutionContextAccessor executionContextAccessor)
    : ICommandHandler<VoidPaymentCommand, VoidPaymentResult>
{
    public async Task<VoidPaymentResult> Handle(
        VoidPaymentCommand command,
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
            PaymentOperations.Void,
            command.IdempotencyKey,
            IdempotencyRequestHasher.HashParts(
                command.MerchantId.Trim(),
                command.PaymentId.ToString("D")),
            now);

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var idempotency = await idempotencyStore.TryBeginAsync<VoidPaymentResult>(
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

            payment.Void(now);

            await paymentRepository.UpdateAsync(payment, cancellationToken);

            var auditEntries = paymentAuditLogFactory.Create(payment, executionContext);
            await auditLogWriter.WriteAsync(auditEntries, cancellationToken);

            var result = new VoidPaymentResult(
                payment.Id.Value,
                payment.MerchantId.Value,
                payment.Status.ToString(),
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
