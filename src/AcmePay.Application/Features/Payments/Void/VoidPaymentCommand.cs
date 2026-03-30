using AcmePay.Application.Abstractions.Messaging;

namespace AcmePay.Application.Features.Payments.Void;

public sealed record VoidPaymentCommand(
    string MerchantId,
    Guid PaymentId,
    string IdempotencyKey) : ICommand<VoidPaymentResult>;
