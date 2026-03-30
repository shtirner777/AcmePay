namespace AcmePay.Core.Payments.ValueObjects;

public readonly record struct Currency
{
    public Currency(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Currency code cannot be empty.", nameof(code));
        }

        code = code.Trim().ToUpperInvariant();

        if (code.Length != 3 || !code.All(char.IsLetter))
        {
            throw new ArgumentException("Currency code must be a 3-letter ISO-like code.", nameof(code));
        }

        Code = code;
    }

    public string Code { get; }

    public override string ToString() => Code;
}