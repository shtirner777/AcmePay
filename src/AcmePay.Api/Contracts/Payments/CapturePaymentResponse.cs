namespace AcmePay.Api.Contracts.Payments;

public sealed record CapturePaymentResponse(
    Guid PaymentId,
    string MerchantId,
    decimal CapturedAmount,
    decimal TotalCapturedAmount,
    decimal RemainingAuthorizedAmount,
    string Currency,
    string Status,
    string CaptureReference,
    DateTimeOffset CapturedAtUtc);
