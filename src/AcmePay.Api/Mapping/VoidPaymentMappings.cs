using AcmePay.Api.Contracts.Payments;
using AcmePay.Application.Features.Payments.Void;

namespace AcmePay.Api.Mapping;

public static class VoidPaymentMappings
{
    public static VoidPaymentCommand ToCommand(
        string merchantId,
        Guid paymentId,
        string idempotencyKey)
    {
        return new VoidPaymentCommand(
            MerchantId: merchantId,
            PaymentId: paymentId,
            IdempotencyKey: idempotencyKey);
    }

    public static VoidPaymentResponse ToResponse(this VoidPaymentResult result)
    {
        return new VoidPaymentResponse(
            PaymentId: result.PaymentId,
            MerchantId: result.MerchantId,
            Status: result.Status,
            VoidedAtUtc: result.VoidedAtUtc);
    }
}
