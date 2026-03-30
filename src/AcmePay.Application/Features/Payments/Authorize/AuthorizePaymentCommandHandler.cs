using System.Globalization;
using AcmePay.Application.Abstractions.Context;
using AcmePay.Application.Abstractions.Messaging;
using AcmePay.Application.Abstractions.Persistence;
using AcmePay.Application.Abstractions.Time;
using AcmePay.Application.Exceptions;
using AcmePay.Application.Payments.Audit;
using AcmePay.Application.Payments.Gateways;
using AcmePay.Application.Payments.Idempotency;
using AcmePay.Application.Payments.Repositories;
using AcmePay.Core.Payments.Aggregates;
using AcmePay.Core.Payments.ValueObjects;
using FluentValidation;

namespace AcmePay.Application.Features.Payments.Authorize;

public sealed class AuthorizePaymentCommandHandler(
    IValidator<AuthorizePaymentCommand> validator,
    IPaymentRepository paymentRepository,
    IIdempotencyStore idempotencyStore,
    IAuditLogWriter auditLogWriter,
    IPaymentAuditLogFactory paymentAuditLogFactory,
    ICardNetworkGateway cardNetworkGateway,
    IUnitOfWork unitOfWork,
    IClock clock,
    IExecutionContextAccessor executionContextAccessor)
    : ICommandHandler<AuthorizePaymentCommand, AuthorizePaymentResult>
{
    public async Task<AuthorizePaymentResult> Handle(
        AuthorizePaymentCommand command,
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
            PaymentOperations.Authorize,
            command.IdempotencyKey,
            IdempotencyRequestHasher.HashParts(
                command.MerchantId.Trim(),
                command.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                command.Currency.Trim().ToUpperInvariant(),
                command.CardholderName.Trim(),
                command.Pan.Trim(),
                command.ExpiryMonth.ToString(CultureInfo.InvariantCulture),
                command.ExpiryYear.ToString(CultureInfo.InvariantCulture),
                command.Cvv.Trim()),
            now);

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var idempotency = await idempotencyStore.TryBeginAsync<AuthorizePaymentResult>(
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

            var gatewayResult = await cardNetworkGateway.AuthorizeAsync(
                new CardAuthorizationRequest(
                    command.MerchantId,
                    command.Amount,
                    command.Currency,
                    command.CardholderName,
                    command.Pan,
                    command.ExpiryMonth,
                    command.ExpiryYear,
                    command.Cvv),
                cancellationToken);

            if (!gatewayResult.IsApproved)
            {
                throw new PaymentAuthorizationFailedException(
                    gatewayResult.DeclineReason ?? "Authorization was declined.");
            }

            var payment = CreatePayment(command, gatewayResult, now);

            await paymentRepository.AddAsync(payment, cancellationToken);

            var auditEntries = paymentAuditLogFactory.Create(payment, executionContext);
            await auditLogWriter.WriteAsync(auditEntries, cancellationToken);

            var result = new AuthorizePaymentResult(
                payment.Id.Value,
                payment.MerchantId.Value,
                payment.AuthorizedAmount.Amount,
                payment.AuthorizedAmount.Currency.Code,
                payment.Status.ToString(),
                payment.AuthorizationReference.Value,
                payment.AuthorizedAtUtc);

            await idempotencyStore.CompleteAsync(
                idempotencyRequest,
                result,
                cancellationToken);

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

    private static Payment CreatePayment(
        AuthorizePaymentCommand command,
        CardAuthorizationResult gatewayResult,
        DateTimeOffset authorizedAtUtc)
    {
        var amount = new Money(
            command.Amount,
            new Currency(command.Currency));

        var paymentMethod = new PaymentMethodSnapshot(
            gatewayResult.Network,
            command.CardholderName,
            new MaskedPan(gatewayResult.MaskedPan),
            new CardFingerprint(gatewayResult.CardFingerprint),
            command.ExpiryMonth,
            command.ExpiryYear);

        return Payment.Authorize(
            new MerchantId(command.MerchantId),
            paymentMethod,
            amount,
            new AuthorizationReference(gatewayResult.AuthorizationReference),
            authorizedAtUtc);
    }
}
