namespace AcmePay.Api.Contracts.Payments;

public sealed record RefundPaymentResponse(
    Guid PaymentId,
    string MerchantId,
    decimal RefundedAmount,
    decimal TotalRefundedAmount,
    decimal RemainingRefundableAmount,
    string Currency,
    string Status,
    string RefundReference,
    DateTimeOffset RefundedAtUtc);
