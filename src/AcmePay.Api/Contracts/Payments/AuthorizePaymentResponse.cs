namespace AcmePay.Api.Contracts.Payments;

public sealed record AuthorizePaymentResponse(
    Guid PaymentId,
    string MerchantId,
    decimal AuthorizedAmount,
    string Currency,
    string Status,
    string AuthorizationReference,
    DateTimeOffset AuthorizedAtUtc);