namespace AcmePay.Core.Payments.ValueObjects;

public readonly record struct AuthorizationReference
{
    public AuthorizationReference(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Authorization reference cannot be empty.", nameof(value));
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;
}