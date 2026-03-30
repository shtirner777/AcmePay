namespace AcmePay.Application.Features.Payments.Capture;

public sealed record CapturePaymentResult(
    Guid PaymentId,
    string MerchantId,
    decimal CapturedAmount,
    decimal TotalCapturedAmount,
    decimal RemainingAuthorizedAmount,
    string Currency,
    string Status,
    string CaptureReference,
    DateTimeOffset CapturedAtUtc);
