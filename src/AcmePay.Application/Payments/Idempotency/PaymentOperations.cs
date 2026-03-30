namespace AcmePay.Application.Payments.Idempotency;

public static class PaymentOperations
{
    public const string Authorize = "authorize";
    public const string Capture = "capture";
    public const string Void = "void";
    public const string Refund = "refund";
}
