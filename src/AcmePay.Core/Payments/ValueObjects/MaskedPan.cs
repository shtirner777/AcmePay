namespace AcmePay.Core.Payments.ValueObjects;

public readonly record struct MaskedPan
{
    public MaskedPan(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Masked PAN cannot be empty.", nameof(value));
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;
}