namespace AcmePay.Core.Payments.ValueObjects;

public readonly record struct RefundReference
{
    public RefundReference(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Refund reference cannot be empty.", nameof(value));
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;
}