namespace Venly.Backend.Common.Money;

/// <summary>
/// A currency SendGram can hold a balance in, with the exponent that turns its minor unit into its major
/// one. Deliberately a closed registry rather than an ISO-4217 lookup: a currency present here is one the
/// ledger has accounts for and reconciliation covers, and there is no useful behaviour for any other.
///
/// <para>EUR is absent on purpose. The corridor is GBP/USD -> NGN; adding EUR is a decision with ledger
/// accounts and a bridge pair attached, not a line in a table.</para>
/// </summary>
public sealed class Currency : IEquatable<Currency>
{
    private Currency(string code, int exponent)
    {
        Code = code;
        Exponent = exponent;

        // An exact integer, computed once. Math.Pow returns a double for what is a power of ten, and a
        // double is the wrong type to derive a money factor from.
        var factor = 1L;
        for (var i = 0; i < exponent; i++) factor *= 10;
        MinorUnitFactor = factor;
    }

    /// <summary>The ISO-4217 alphabetic code, uppercase.</summary>
    public string Code { get; }

    /// <summary>Digits after the decimal point: 2 for all three of these.</summary>
    public int Exponent { get; }

    /// <summary>Minor units in one major unit: 100 for a two-exponent currency.</summary>
    public long MinorUnitFactor { get; }

    public static readonly Currency GBP = new("GBP", 2);
    public static readonly Currency USD = new("USD", 2);
    public static readonly Currency NGN = new("NGN", 2);

    private static readonly Dictionary<string, Currency> Registry =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [GBP.Code] = GBP,
            [USD.Code] = USD,
            [NGN.Code] = NGN,
        };

    public static IReadOnlyCollection<Currency> All { get; } = [GBP, USD, NGN];

    /// <summary>Throws rather than returning null: an unrecognised code at a posting site is a bug.</summary>
    public static Currency FromCode(string code) =>
        TryFromCode(code, out var currency)
            ? currency!
            : throw new ArgumentOutOfRangeException(
                nameof(code), code, $"'{code}' is not a currency this ledger holds accounts for.");

    public static bool TryFromCode(string code, out Currency? currency)
    {
        if (!string.IsNullOrWhiteSpace(code) && Registry.TryGetValue(code, out var found))
        {
            currency = found;
            return true;
        }

        currency = null;
        return false;
    }

    public bool Equals(Currency? other) => other is not null && Code == other.Code;
    public override bool Equals(object? obj) => obj is Currency c && Equals(c);
    public override int GetHashCode() => Code.GetHashCode();
    public override string ToString() => Code;

    public static bool operator ==(Currency? a, Currency? b) => Equals(a, b);
    public static bool operator !=(Currency? a, Currency? b) => !Equals(a, b);
}
