using AcmePay.Core.Payments.Enums;

namespace AcmePay.Core.Payments.ValueObjects;

public sealed record PaymentMethodSnapshot
{
    public PaymentMethodSnapshot(
        CardNetwork network,
        string cardholderName,
        MaskedPan maskedPan,
        CardFingerprint fingerprint,
        int expiryMonth,
        int expiryYear)
    {
        if (string.IsNullOrWhiteSpace(cardholderName))
        {
            throw new ArgumentException("Cardholder name cannot be empty.", nameof(cardholderName));
        }

        if (expiryMonth is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(expiryMonth), "Expiry month must be between 1 and 12.");
        }

        if (expiryYear is < 1 or > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(expiryYear), "Expiry year looks invalid.");
        }

        Network = network;
        CardholderName = cardholderName.Trim();
        MaskedPan = maskedPan;
        Fingerprint = fingerprint;
        ExpiryMonth = expiryMonth;
        ExpiryYear = expiryYear;
    }

    public CardNetwork Network { get; }
    public string CardholderName { get; }
    public MaskedPan MaskedPan { get; }
    public CardFingerprint Fingerprint { get; }
    public int ExpiryMonth { get; }
    public int ExpiryYear { get; }
}
