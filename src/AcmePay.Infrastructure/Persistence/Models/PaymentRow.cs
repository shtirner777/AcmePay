namespace AcmePay.Infrastructure.Persistence.Models;

internal sealed class PaymentRow
{
    public Guid PaymentId { get; init; }
    public string MerchantId { get; init; } = default!;
    public int Status { get; init; }

    public decimal AuthorizedAmount { get; init; }
    public decimal CapturedAmount { get; init; }
    public decimal RefundedAmount { get; init; }
    public string Currency { get; init; } = default!;

    public int CardNetwork { get; init; }
    public string CardholderName { get; init; } = default!;
    public string MaskedPan { get; init; } = default!;
    public string CardFingerprint { get; init; } = default!;
    public int ExpiryMonth { get; init; }
    public int ExpiryYear { get; init; }

    public string AuthorizationReference { get; init; } = default!;

    public DateTimeOffset AuthorizedAtUtc { get; init; }
    public DateTimeOffset LastModifiedAtUtc { get; init; }
}