using AcmePay.Api.Contracts.Payments;

namespace AcmePay.IntegrationTests.Payments;

internal static class PaymentApiTestConstants
{
    public const string MerchantId = "merchant-1";
    public const string CurrencyCode = "USD";
    public const string CardholderName = "John Doe";
    public const string VisaPan = "4111111111111111";
    public const string DeclinedVisaPan = "4111111111110000";
    public const int ExpiryMonth = 12;
    public const int ExpiryYear = 2030;
    public const string Cvv = "123";
    public const string TriggeredBy = "merchant-api-integration-tests";
    public const string AuthorizeCorrelationId = "correlation-id-authorize-001";
    public const string LifecycleCorrelationId = "correlation-id-lifecycle-001";
    public const string AuthorizePaymentIdempotencyKey = "authorize-payment-request-1";
    public const string SecondAuthorizePaymentIdempotencyKey = "authorize-payment-request-2";
    public const string CapturePaymentIdempotencyKey = "capture-payment-request-1";
    public const string SecondCapturePaymentIdempotencyKey = "capture-payment-request-2";
    public const string RefundPaymentIdempotencyKey = "refund-payment-request-1";
    public const string SecondRefundPaymentIdempotencyKey = "refund-payment-request-2";
    public const string VoidPaymentIdempotencyKey = "void-payment-request-1";
    public const string AuthorizedStatus = "Authorized";
    public const string VoidedStatus = "Voided";
    public const string PartiallyRefundedStatus = "PartiallyRefunded";

    public static string AuthorizeRoute => $"/api/merchants/{MerchantId}/payments/authorize";
    public static string CaptureRoute(Guid paymentId) => $"/api/merchants/{MerchantId}/payments/{paymentId:D}/capture";
    public static string RefundRoute(Guid paymentId) => $"/api/merchants/{MerchantId}/payments/{paymentId:D}/refund";
    public static string VoidRoute(Guid paymentId) => $"/api/merchants/{MerchantId}/payments/{paymentId:D}/void";

    public static AuthorizePaymentRequest CreateAuthorizeRequest(decimal amount, string? pan = null)
        => new(amount, CurrencyCode, CardholderName, pan ?? VisaPan, ExpiryMonth, ExpiryYear, Cvv);

    public static CapturePaymentRequest CreateCaptureRequest(decimal amount)
        => new(amount);

    public static RefundPaymentRequest CreateRefundRequest(decimal amount)
        => new(amount);
}
