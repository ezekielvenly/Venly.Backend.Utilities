using Venly.Backend.Common.Money;

// Namespace is MoneyValues, not Money, deliberately: inside a namespace whose last segment is "Money"
// the identifier Money binds to the NAMESPACE before any using-alias is consulted, so every
// Money.FromMinor call fails to resolve. Renaming the test namespace is cheaper than distorting the
// production type to suit a test.
namespace Venly.Backend.Utilities.Tests.MoneyValues;

public class CurrencyTests
{
    [Theory]
    [InlineData("GBP")]
    [InlineData("USD")]
    [InlineData("NGN")]
    public void FromCode_returns_the_registered_currency(string code)
    {
        Assert.Equal(code, Currency.FromCode(code).Code);
    }

    [Fact]
    public void FromCode_is_case_insensitive()
    {
        Assert.Equal(Currency.GBP, Currency.FromCode("gbp"));
    }

    [Fact]
    public void FromCode_throws_on_an_unregistered_code()
    {
        // EUR is deliberately absent: the corridor is GBP/USD -> NGN. A currency that silently
        // resolved would post entries into an account no reconciliation covers.
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Currency.FromCode("EUR"));
        Assert.Contains("EUR", ex.Message);
    }

    [Fact]
    public void TryFromCode_reports_failure_without_throwing()
    {
        Assert.False(Currency.TryFromCode("EUR", out var currency));
        Assert.Null(currency);
    }

    [Theory]
    [InlineData("GBP")]
    [InlineData("USD")]
    [InlineData("NGN")]
    public void MinorUnitFactor_is_ten_to_the_exponent(string code)
    {
        var currency = Currency.FromCode(code);
        var expected = (long)System.Math.Pow(10, currency.Exponent);
        Assert.Equal(expected, currency.MinorUnitFactor);
    }

    [Fact]
    public void All_holds_exactly_the_three_corridor_currencies()
    {
        Assert.Equal(new[] { "GBP", "NGN", "USD" }, Currency.All.Select(c => c.Code).OrderBy(c => c));
    }
}
