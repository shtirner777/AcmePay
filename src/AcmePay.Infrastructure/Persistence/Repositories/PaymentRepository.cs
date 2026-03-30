using Dapper;
using AcmePay.Application.Abstractions.Persistence;
using AcmePay.Application.Payments.Repositories;
using AcmePay.Core.Payments.Aggregates;
using AcmePay.Core.Payments.Enums;
using AcmePay.Core.Payments.ValueObjects;
using AcmePay.Infrastructure.Persistence.Models;

namespace AcmePay.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository(IUnitOfWork unitOfWork) : IPaymentRepository
{
    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into payments
            (
                payment_id,
                merchant_id,
                status,
                authorized_amount,
                captured_amount,
                refunded_amount,
                currency,
                card_network,
                cardholder_name,
                masked_pan,
                card_fingerprint,
                expiry_month,
                expiry_year,
                authorization_reference,
                authorized_at_utc,
                last_modified_at_utc
            )
            values
            (
                @PaymentId,
                @MerchantId,
                @Status,
                @AuthorizedAmount,
                @CapturedAmount,
                @RefundedAmount,
                @Currency,
                @CardNetwork,
                @CardholderName,
                @MaskedPan,
                @CardFingerprint,
                @ExpiryMonth,
                @ExpiryYear,
                @AuthorizationReference,
                @AuthorizedAtUtc,
                @LastModifiedAtUtc
            );
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                PaymentId = payment.Id.Value,
                MerchantId = payment.MerchantId.Value,
                Status = (int)payment.Status,
                AuthorizedAmount = payment.AuthorizedAmount.Amount,
                CapturedAmount = payment.CapturedAmount.Amount,
                RefundedAmount = payment.RefundedAmount.Amount,
                Currency = payment.AuthorizedAmount.Currency.Code,
                CardNetwork = (int)payment.PaymentMethod.Network,
                CardholderName = payment.PaymentMethod.CardholderName,
                MaskedPan = payment.PaymentMethod.MaskedPan.Value,
                CardFingerprint = payment.PaymentMethod.Fingerprint.Value,
                ExpiryMonth = payment.PaymentMethod.ExpiryMonth,
                ExpiryYear = payment.PaymentMethod.ExpiryYear,
                AuthorizationReference = payment.AuthorizationReference.Value,
                AuthorizedAtUtc = payment.AuthorizedAtUtc,
                LastModifiedAtUtc = payment.LastModifiedAtUtc
            },
            transaction: unitOfWork.Transaction,
            cancellationToken: cancellationToken);

        await unitOfWork.Connection.ExecuteAsync(command);
    }

    public async Task<Payment?> GetByIdAsync(PaymentId paymentId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select
                payment_id as PaymentId,
                merchant_id as MerchantId,
                status as Status,
                authorized_amount as AuthorizedAmount,
                captured_amount as CapturedAmount,
                refunded_amount as RefundedAmount,
                currency as Currency,
                card_network as CardNetwork,
                cardholder_name as CardholderName,
                masked_pan as MaskedPan,
                card_fingerprint as CardFingerprint,
                expiry_month as ExpiryMonth,
                expiry_year as ExpiryYear,
                authorization_reference as AuthorizationReference,
                authorized_at_utc as AuthorizedAtUtc,
                last_modified_at_utc as LastModifiedAtUtc
            from payments
            where payment_id = @PaymentId;
            """;

        var command = new CommandDefinition(
            sql,
            new { PaymentId = paymentId.Value },
            transaction: unitOfWork.Transaction,
            cancellationToken: cancellationToken);

        var row = await unitOfWork.Connection.QuerySingleOrDefaultAsync<PaymentRow>(command);
        return row is null ? null : Map(row);
    }

    public async Task<Payment?> GetForUpdateAsync(PaymentId paymentId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select
                payment_id as PaymentId,
                merchant_id as MerchantId,
                status as Status,
                authorized_amount as AuthorizedAmount,
                captured_amount as CapturedAmount,
                refunded_amount as RefundedAmount,
                currency as Currency,
                card_network as CardNetwork,
                cardholder_name as CardholderName,
                masked_pan as MaskedPan,
                card_fingerprint as CardFingerprint,
                expiry_month as ExpiryMonth,
                expiry_year as ExpiryYear,
                authorization_reference as AuthorizationReference,
                authorized_at_utc as AuthorizedAtUtc,
                last_modified_at_utc as LastModifiedAtUtc
            from payments
            where payment_id = @PaymentId
            for update;
            """;

        var command = new CommandDefinition(
            sql,
            new { PaymentId = paymentId.Value },
            transaction: unitOfWork.Transaction,
            cancellationToken: cancellationToken);

        var row = await unitOfWork.Connection.QuerySingleOrDefaultAsync<PaymentRow>(command);
        return row is null ? null : Map(row);
    }

    public async Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update payments
            set
                status = @Status,
                captured_amount = @CapturedAmount,
                refunded_amount = @RefundedAmount,
                last_modified_at_utc = @LastModifiedAtUtc
            where payment_id = @PaymentId;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                PaymentId = payment.Id.Value,
                Status = (int)payment.Status,
                CapturedAmount = payment.CapturedAmount.Amount,
                RefundedAmount = payment.RefundedAmount.Amount,
                LastModifiedAtUtc = payment.LastModifiedAtUtc
            },
            transaction: unitOfWork.Transaction,
            cancellationToken: cancellationToken);

        await unitOfWork.Connection.ExecuteAsync(command);
    }

    private static Payment Map(PaymentRow row)
    {
        var currency = new Currency(row.Currency);

        return Payment.Rehydrate(
            new PaymentId(row.PaymentId),
            new MerchantId(row.MerchantId),
            (PaymentStatus)row.Status,
            new PaymentMethodSnapshot(
                (CardNetwork)row.CardNetwork,
                row.CardholderName,
                new MaskedPan(row.MaskedPan),
                new CardFingerprint(row.CardFingerprint),
                row.ExpiryMonth,
                row.ExpiryYear),
            new Money(row.AuthorizedAmount, currency),
            new Money(row.CapturedAmount, currency),
            new Money(row.RefundedAmount, currency),
            new AuthorizationReference(row.AuthorizationReference),
            row.AuthorizedAtUtc,
            row.LastModifiedAtUtc);
    }
}