namespace AcmePay.Core.Payments.Errors;

public static class PaymentErrors
{
    public const string AmountMustBeGreaterThanZero = "Amount must be greater than zero.";
    public const string AmountCannotBeNegative = "Amount cannot be negative.";
    public const string CurrencyMismatch = "Money currency mismatch.";
    public const string PaymentCannotBeVoided = "Payment cannot be voided in its current state.";
    public const string PaymentCannotBeCaptured = "Payment cannot be captured in its current state.";
    public const string PaymentCannotBeRefunded = "Payment cannot be refunded in its current state.";
    public const string CaptureAmountExceedsRemainingAuthorizedAmount =
        "Capture amount exceeds remaining authorized amount.";
    public const string RefundAmountExceedsRemainingCapturedAmount =
        "Refund amount exceeds remaining refundable amount.";
}