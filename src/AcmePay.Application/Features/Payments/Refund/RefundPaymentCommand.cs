using AcmePay.Application.Abstractions.Messaging;

namespace AcmePay.Application.Features.Payments.Refund;

public sealed record RefundPaymentCommand(
    string MerchantId,
    Guid PaymentId,
    string IdempotencyKey,
    decimal Amount) : ICommand<RefundPaymentResult>;
