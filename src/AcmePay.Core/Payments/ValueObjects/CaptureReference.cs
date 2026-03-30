namespace AcmePay.Core.Payments.ValueObjects;

public readonly record struct CaptureReference
{
    public CaptureReference(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Capture reference cannot be empty.", nameof(value));
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;
}