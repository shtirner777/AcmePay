using AcmePay.Api.Contracts.Payments;
using AcmePay.Application.Features.Payments.Refund;

namespace AcmePay.Api.Mapping;

public static class RefundPaymentMappings
{
    public static RefundPaymentCommand ToCommand(
        this RefundPaymentRequest request,
        string merchantId,
        Guid paymentId,
        string idempotencyKey)
    {
        return new RefundPaymentCommand(
            MerchantId: merchantId,
            PaymentId: paymentId,
            IdempotencyKey: idempotencyKey,
            Amount: request.Amount);
    }

    public static RefundPaymentResponse ToResponse(this RefundPaymentResult result)
    {
        return new RefundPaymentResponse(
            PaymentId: result.PaymentId,
            MerchantId: result.MerchantId,
            RefundedAmount: result.RefundedAmount,
            TotalRefundedAmount: result.TotalRefundedAmount,
            RemainingRefundableAmount: result.RemainingRefundableAmount,
            Currency: result.Currency,
            Status: result.Status,
            RefundReference: result.RefundReference,
            RefundedAtUtc: result.RefundedAtUtc);
    }
}
