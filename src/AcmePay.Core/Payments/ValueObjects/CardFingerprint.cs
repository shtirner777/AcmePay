namespace AcmePay.Core.Payments.ValueObjects;

public readonly record struct CardFingerprint
{
    public CardFingerprint(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Card fingerprint cannot be empty.", nameof(value));
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;
}