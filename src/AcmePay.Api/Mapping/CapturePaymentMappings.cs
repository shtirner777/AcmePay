using AcmePay.Api.Contracts.Payments;
using AcmePay.Application.Features.Payments.Capture;

namespace AcmePay.Api.Mapping;

public static class CapturePaymentMappings
{
    public static CapturePaymentCommand ToCommand(
        this CapturePaymentRequest request,
        string merchantId,
        Guid paymentId,
        string idempotencyKey)
    {
        return new CapturePaymentCommand(
            MerchantId: merchantId,
            PaymentId: paymentId,
            IdempotencyKey: idempotencyKey,
            Amount: request.Amount);
    }

    public static CapturePaymentResponse ToResponse(this CapturePaymentResult result)
    {
        return new CapturePaymentResponse(
            PaymentId: result.PaymentId,
            MerchantId: result.MerchantId,
            CapturedAmount: result.CapturedAmount,
            TotalCapturedAmount: result.TotalCapturedAmount,
            RemainingAuthorizedAmount: result.RemainingAuthorizedAmount,
            Currency: result.Currency,
            Status: result.Status,
            CaptureReference: result.CaptureReference,
            CapturedAtUtc: result.CapturedAtUtc);
    }
}
