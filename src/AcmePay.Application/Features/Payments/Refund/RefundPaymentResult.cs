namespace AcmePay.Application.Features.Payments.Refund;

public sealed record RefundPaymentResult(
    Guid PaymentId,
    string MerchantId,
    decimal RefundedAmount,
    decimal TotalRefundedAmount,
    decimal RemainingRefundableAmount,
    string Currency,
    string Status,
    string RefundReference,
    DateTimeOffset RefundedAtUtc);
