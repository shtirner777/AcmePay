namespace AcmePay.Application.Features.Payments.Authorize;

public sealed record AuthorizePaymentResult(
    Guid PaymentId,
    string MerchantId,
    decimal AuthorizedAmount,
    string Currency,
    string Status,
    string AuthorizationReference,
    DateTimeOffset AuthorizedAtUtc);