namespace AcmePay.Api.Contracts.Payments;

public sealed record AuthorizePaymentRequest(
    decimal Amount,
    string Currency,
    string CardholderName,
    string Pan,
    int ExpiryMonth,
    int ExpiryYear,
    string Cvv);