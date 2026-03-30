namespace AcmePay.Core.Payments.ValueObjects;

public readonly record struct MerchantId
{
    public MerchantId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("MerchantId cannot be empty.", nameof(value));
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;
}