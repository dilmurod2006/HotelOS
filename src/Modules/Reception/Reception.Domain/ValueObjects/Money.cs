namespace Reception.Domain.ValueObjects;

/// <summary>
/// Value Object: Money.
///
/// Never use float/double for monetary values — floating-point arithmetic introduces
/// rounding errors that compound over billing cycles. Decimal is exact.
/// Immutability is enforced: operations return new Money instances.
/// </summary>
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Money amount cannot be negative.");
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO 4217 code.", nameof(currency));

        Amount = decimal.Round(amount, 2);
        Currency = currency.ToUpperInvariant();
    }

    public static Money Of(decimal amount, string currency = "USD") => new(amount, currency);
    public static Money Zero(string currency = "USD") => new(0m, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Multiply(int factor) => new(Amount * factor, Currency);
    public Money Multiply(decimal factor) => new(Amount * factor, Currency);

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot combine {Currency} and {other.Currency}.");
    }

    public override string ToString() => $"{Currency} {Amount:F2}";
}
