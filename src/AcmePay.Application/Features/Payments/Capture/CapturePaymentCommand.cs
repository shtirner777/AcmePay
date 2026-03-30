using AcmePay.Application.Abstractions.Messaging;

namespace AcmePay.Application.Features.Payments.Capture;

public sealed record CapturePaymentCommand(
    string MerchantId,
    Guid PaymentId,
    string IdempotencyKey,
    decimal Amount) : ICommand<CapturePaymentResult>;
