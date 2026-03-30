using AcmePay.Application.Abstractions.Messaging;

namespace AcmePay.Application.Features.Payments.Authorize;

public sealed record AuthorizePaymentCommand(
    string MerchantId,
    string IdempotencyKey,
    decimal Amount,
    string Currency,
    string CardholderName,
    string Pan,
    int ExpiryMonth,
    int ExpiryYear,
    string Cvv) : ICommand<AuthorizePaymentResult>;