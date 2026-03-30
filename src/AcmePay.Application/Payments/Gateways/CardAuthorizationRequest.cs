namespace AcmePay.Application.Payments.Gateways;

public sealed record CardAuthorizationRequest(
    string MerchantId,
    decimal Amount,
    string Currency,
    string CardholderName,
    string Pan,
    int ExpiryMonth,
    int ExpiryYear,
    string Cvv);