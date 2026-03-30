namespace AcmePay.UnitTests.Payments;

internal static class PaymentTestConstants
{
    public const string MerchantId = "merchant-1";
    public const string CurrencyCode = "USD";
    public const string CardholderName = "John Doe";
    public const string VisaPan = "4111111111111111";
    public const string DeclinedVisaPan = "4111111111110000";
    public const int ExpiryMonth = 12;
    public const int ExpiryYear = 2030;
    public const string Cvv = "123";
    public const string TriggeredBy = "merchant-api";
    public const string CorrelationId = "correlation-id-001";
    public const string AuthorizePaymentIdempotencyKey = "authorize-payment-request-1";
    public const string CapturePaymentIdempotencyKey = "capture-payment-request-1";
    public const string RefundPaymentIdempotencyKey = "refund-payment-request-1";
    public const string VoidPaymentIdempotencyKey = "void-payment-request-1";
    public const string AuthorizedStatus = "Authorized";
    public const string VoidedStatus = "Voided";
    public const string PartiallyRefundedStatus = "PartiallyRefunded";
    public static readonly Guid CachedPaymentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
}
