using Venly.Backend.Common.Money;
using Money = Venly.Backend.Common.Money.Money;

// Namespace is MoneyValues, not Money, deliberately: inside a namespace whose last segment is "Money"
// the identifier Money binds to the NAMESPACE before any using-alias is consulted, so every
// Money.FromMinor call fails to resolve. Renaming the test namespace is cheaper than distorting the
// production type to suit a test.
namespace Venly.Backend.Utilities.Tests.MoneyValues;

public class MoneyTests
{
    [Fact]
    public void FromDecimal_rounds_it_does_not_truncate()
    {
        // The RefProj defect this test exists for: a (long) cast truncates toward zero, so 2.999
        // became 2.99 and the penny was gone with nothing recording it.
        Assert.Equal(300, Money.FromDecimal(2.999m, Currency.GBP).Minor);
        Assert.Equal(299, Money.FromDecimal(2.994m, Currency.GBP).Minor);
    }

    [Fact]
    public void FromDecimal_rounds_half_away_from_zero_in_both_directions()
    {
        Assert.Equal(3, Money.FromDecimal(0.025m, Currency.GBP).Minor);
        Assert.Equal(-3, Money.FromDecimal(-0.025m, Currency.GBP).Minor);
    }

    [Fact]
    public void ToDecimal_round_trips_a_minor_amount()
    {
        Assert.Equal(123.45m, Money.FromMinor(12345, Currency.GBP).ToDecimal());
    }

    [Fact]
    public void Subtraction_yields_a_negative_amount_rather_than_throwing()
    {
        // RefProj threw "Insufficient funds" from operator-. Sufficiency is a handler's rule; a
        // ledger needs signed deltas, and a reversal is exactly a negative delta.
        var result = Money.FromMinor(100, Currency.GBP) - Money.FromMinor(250, Currency.GBP);
        Assert.Equal(-150, result.Minor);
    }

    [Fact]
    public void Addition_and_subtraction_refuse_a_currency_mismatch()
    {
        var gbp = Money.FromMinor(100, Currency.GBP);
        var ngn = Money.FromMinor(100, Currency.NGN);

        Assert.Throws<InvalidOperationException>(() => { _ = gbp + ngn; });
        Assert.Throws<InvalidOperationException>(() => { _ = gbp - ngn; });
    }

    [Fact]
    public void Comparison_operators_are_all_present_and_consistent()
    {
        var small = Money.FromMinor(100, Currency.GBP);
        var large = Money.FromMinor(200, Currency.GBP);

        Assert.True(small < large);
        Assert.True(small <= large);
        Assert.False(small > large);
        Assert.False(small >= large);
        Assert.True(small != large);
        Assert.True(small == Money.FromMinor(100, Currency.GBP));
    }

    [Fact]
    public void Comparison_refuses_a_currency_mismatch()
    {
        var gbp = Money.FromMinor(100, Currency.GBP);
        var ngn = Money.FromMinor(100, Currency.NGN);

        Assert.Throws<InvalidOperationException>(() => { _ = gbp < ngn; });
    }

    [Fact]
    public void Equality_across_currencies_is_false_and_does_not_throw()
    {
        // == is value equality, not an ordering, so it must answer rather than throw -- otherwise
        // Money cannot go in a Dictionary or a HashSet.
        Assert.False(Money.FromMinor(100, Currency.GBP) == Money.FromMinor(100, Currency.NGN));
    }

    [Fact]
    public void Addition_overflow_throws_rather_than_wrapping()
    {
        var max = Money.FromMinor(long.MaxValue, Currency.NGN);
        Assert.Throws<OverflowException>(() => { _ = max + Money.FromMinor(1, Currency.NGN); });
    }

    [Fact]
    public void ToString_shows_the_major_amount_and_the_code()
    {
        Assert.Equal("123.45 GBP", Money.FromMinor(12345, Currency.GBP).ToString());
    }
}
