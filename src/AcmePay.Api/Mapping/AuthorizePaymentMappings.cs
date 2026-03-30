using AcmePay.Api.Contracts.Payments;
using AcmePay.Application.Features.Payments.Authorize;

namespace AcmePay.Api.Mapping;

public static class AuthorizePaymentMappings
{
    public static AuthorizePaymentCommand ToCommand(
        this AuthorizePaymentRequest request,
        string merchantId,
        string idempotencyKey)
    {
        return new AuthorizePaymentCommand(
            MerchantId: merchantId,
            IdempotencyKey: idempotencyKey,
            Amount: request.Amount,
            Currency: request.Currency,
            CardholderName: request.CardholderName,
            Pan: request.Pan,
            ExpiryMonth: request.ExpiryMonth,
            ExpiryYear: request.ExpiryYear,
            Cvv: request.Cvv);
    }

    public static AuthorizePaymentResponse ToResponse(this AuthorizePaymentResult result)
    {
        return new AuthorizePaymentResponse(
            PaymentId: result.PaymentId,
            MerchantId: result.MerchantId,
            AuthorizedAmount: result.AuthorizedAmount,
            Currency: result.Currency,
            Status: result.Status,
            AuthorizationReference: result.AuthorizationReference,
            AuthorizedAtUtc: result.AuthorizedAtUtc);
    }
}