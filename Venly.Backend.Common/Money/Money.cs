using System.Globalization;

namespace Venly.Backend.Common.Money;

/// <summary>
/// An amount in a currency's MINOR unit. Pence, cents, kobo — never a fractional major unit, because
/// binary floating point cannot represent one and <c>decimal</c> arithmetic accumulates a rounding
/// decision at every step instead of at the one boundary where a human chose it.
///
/// <para>A struct, and immutable: a <see cref="Money"/> is a value the way an <c>int</c> is, and a
/// balance field that could be mutated in place is a field two callers can disagree about.</para>
///
/// <para>Arithmetic is CHECKED. An overflow in a money type that wrapped silently would produce a
/// balance of the opposite sign, and every downstream check would pass.</para>
/// </summary>
public readonly record struct Money
{
    private Money(long minor, Currency currency)
    {
        Minor = minor;
        Currency = currency;
    }

    /// <summary>The amount in minor units. Signed — a reversal is a negative delta.</summary>
    public long Minor { get; }

    public Currency Currency { get; }

    public static Money FromMinor(long minor, Currency currency) =>
        new(minor, currency ?? throw new ArgumentNullException(nameof(currency)));

    /// <summary>
    /// Converts a major-unit decimal, ROUNDING half away from zero. Never truncating: a cast to
    /// <c>long</c> drops the fraction toward zero, which silently loses a penny on every amount that
    /// carries one and loses it in the house's favour on a credit and the customer's on a debit.
    /// </summary>
    public static Money FromDecimal(decimal value, Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);

        var scaled = value * currency.MinorUnitFactor;
        var rounded = Math.Round(scaled, MidpointRounding.AwayFromZero);

        return new Money(checked((long)rounded), currency);
    }

    public static Money Zero(Currency currency) => FromMinor(0, currency);

    public decimal ToDecimal() => (decimal)Minor / Currency.MinorUnitFactor;

    public Money Abs() => new(Math.Abs(Minor), Currency);

    public bool IsNegative => Minor < 0;
    public bool IsZero => Minor == 0;

    public static Money operator +(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return new Money(checked(a.Minor + b.Minor), a.Currency);
    }

    /// <summary>
    /// May return a NEGATIVE amount. Sufficiency is a business rule a handler applies with a message a
    /// customer can act on; arithmetic that threw here would make an overdrawn intermediate value
    /// impossible to compute at all.
    /// </summary>
    public static Money operator -(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return new Money(checked(a.Minor - b.Minor), a.Currency);
    }

    public static Money operator -(Money a) => new(checked(-a.Minor), a.Currency);

    public static bool operator <(Money a, Money b)  { EnsureSameCurrency(a, b); return a.Minor <  b.Minor; }
    public static bool operator >(Money a, Money b)  { EnsureSameCurrency(a, b); return a.Minor >  b.Minor; }
    public static bool operator <=(Money a, Money b) { EnsureSameCurrency(a, b); return a.Minor <= b.Minor; }
    public static bool operator >=(Money a, Money b) { EnsureSameCurrency(a, b); return a.Minor >= b.Minor; }

    // == and != come from the record struct: value equality over (Minor, Currency). They must ANSWER
    // across currencies rather than throw, or Money could not be a dictionary key. Only the ORDERING
    // operators refuse a mismatch, because "is 1 GBP more than 1 NGN" has no answer without a rate.

    private static void EnsureSameCurrency(Money a, Money b)
    {
        if (a.Currency != b.Currency)
        {
            throw new InvalidOperationException(
                $"Currency mismatch: {a.Currency.Code} and {b.Currency.Code}. A cross-currency amount is "
                + "two separately balanced legs joined by an intent, never one arithmetic expression.");
        }
    }

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{ToDecimal():0.00} {Currency.Code}");
}
