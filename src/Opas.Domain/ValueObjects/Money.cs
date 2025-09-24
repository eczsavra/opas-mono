using Opas.Domain.Primitives;

namespace Opas.Domain.ValueObjects;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; } // ISO 4217 (ör. TRY, USD)

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public static bool TryCreate(decimal amount, string? currency, out Money? money, out string? error)
    {
        money = null; error = null;
        if (string.IsNullOrWhiteSpace(currency) || currency!.Length != 3) { error = "currency must be 3-letter ISO."; return false; }
        if (amount < 0) { error = "amount cannot be negative."; return false; }
        money = new Money(decimal.Round(amount, 2, MidpointRounding.AwayFromZero), currency!);
        return true;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
