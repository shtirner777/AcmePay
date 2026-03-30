using AcmePay.Application.Features.Payments.Authorize;
using AcmePay.Core.Payments.Aggregates;
using AcmePay.Core.Payments.Enums;
using AcmePay.Core.Payments.ValueObjects;

namespace AcmePay.UnitTests.Payments;

internal static class PaymentTestData
{
    public static AuthorizePaymentCommand CreateAuthorizeCommand(
        decimal amount = 42.50m,
        string? merchantId = null,
        string? idempotencyKey = null,
        string? currency = null,
        string? cardholderName = null,
        string? pan = null,
        int? expiryMonth = null,
        int? expiryYear = null,
        string? cvv = null)
    {
        return new AuthorizePaymentCommand(
            MerchantId: merchantId ?? PaymentTestConstants.MerchantId,
            IdempotencyKey: idempotencyKey ?? PaymentTestConstants.AuthorizePaymentIdempotencyKey,
            Amount: amount,
            Currency: currency ?? PaymentTestConstants.CurrencyCode,
            CardholderName: cardholderName ?? PaymentTestConstants.CardholderName,
            Pan: pan ?? PaymentTestConstants.VisaPan,
            ExpiryMonth: expiryMonth ?? PaymentTestConstants.ExpiryMonth,
            ExpiryYear: expiryYear ?? PaymentTestConstants.ExpiryYear,
            Cvv: cvv ?? PaymentTestConstants.Cvv);
    }

    public static Payment CreateAuthorizedPayment(
        decimal amount,
        DateTimeOffset? authorizedAtUtc = null,
        string? merchantId = null)
    {
        return Payment.Authorize(
            new MerchantId(merchantId ?? PaymentTestConstants.MerchantId),
            CreatePaymentMethodSnapshot(),
            new Money(amount, new Currency(PaymentTestConstants.CurrencyCode)),
            new AuthorizationReference("AUTH-123456789012"),
            authorizedAtUtc ?? new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero));
    }

    public static PaymentMethodSnapshot CreatePaymentMethodSnapshot()
    {
        return new PaymentMethodSnapshot(
            CardNetwork.Visa,
            PaymentTestConstants.CardholderName,
            new MaskedPan("**** **** **** 4242"),
            new CardFingerprint("0123456789ABCDEF0123456789ABCDEF"),
            PaymentTestConstants.ExpiryMonth,
            PaymentTestConstants.ExpiryYear);
    }
}
