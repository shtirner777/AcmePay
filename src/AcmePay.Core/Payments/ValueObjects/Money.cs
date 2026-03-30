using AcmePay.Core.Exceptions;
using AcmePay.Core.Payments.Errors;

namespace AcmePay.Core.Payments.ValueObjects;

public readonly record struct Money : IComparable<Money>
{
    public Money(decimal amount, Currency currency)
    {
        if (amount < 0)
        {
            throw new DomainRuleViolationException(PaymentErrors.AmountCannotBeNegative);
        }

        Amount = decimal.Round(amount, 2, MidpointRounding.ToEven);
        Currency = currency;
    }

    public decimal Amount { get; }
    public Currency Currency { get; }

    public bool IsZero => Amount == 0m;

    public static Money Zero(Currency currency) => new(0m, currency);

    public int CompareTo(Money other)
    {
        EnsureSameCurrency(other);
        return Amount.CompareTo(other.Amount);
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);

        var result = Amount - other.Amount;
        if (result < 0)
        {
            throw new DomainRuleViolationException(PaymentErrors.AmountCannotBeNegative);
        }

        return new Money(result, Currency);
    }

    public static Money operator +(Money left, Money right) => left.Add(right);
    public static Money operator -(Money left, Money right) => left.Subtract(right);
    public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;
    public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;
    public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;
    public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new DomainRuleViolationException(PaymentErrors.CurrencyMismatch);
        }
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}