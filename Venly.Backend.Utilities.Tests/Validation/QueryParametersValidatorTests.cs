using Venly.Backend.Common;
using Venly.Backend.Common.Validation;

namespace Venly.Backend.Utilities.Tests.Validation;

public class QueryParametersValidatorTests
{
    private sealed class Parameters : QueryParameters;

    private sealed class Validator : QueryParametersValidator<Parameters>;

    private static readonly Validator Subject = new();

    [Fact]
    public void The_defaults_are_valid()
    {
        Assert.True(Subject.Validate(new Parameters()).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_page_number_below_one_is_refused(int pageNo)
    {
        // Skip((PageNo - 1) * PageSize) with PageNo 0 is Skip(-10), which throws
        // ArgumentOutOfRangeException. Validating turns a 500 into a 400.
        var result = Subject.Validate(new Parameters { PageNo = pageNo });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(QueryParameters.PageNo));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void A_page_size_below_one_is_refused(int pageSize)
    {
        var result = Subject.Validate(new Parameters { PageSize = pageSize });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(QueryParameters.PageSize));
    }

    [Fact]
    public void An_over_large_page_size_is_valid_because_the_setter_already_clamped_it()
    {
        var parameters = new Parameters { PageSize = 5_000 };

        // QueryParameters caps at 100 in its setter, so by the time a validator sees it the value is legal.
        // A rule for this could never fire.
        Assert.Equal(100, parameters.PageSize);
        Assert.True(Subject.Validate(parameters).IsValid);
    }

    [Fact]
    public void A_reversed_date_range_is_refused()
    {
        var result = Subject.Validate(new Parameters
        {
            StartDate = new DateOnly(2026, 8, 25),
            EndDate = new DateOnly(2026, 8, 20),
        });

        // Otherwise it returns zero rows, which reads as "nothing happened" rather than "your filter is
        // backwards".
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(QueryParameters.StartDate));
    }

    [Fact]
    public void The_same_day_for_both_bounds_is_valid()
    {
        var result = Subject.Validate(new Parameters
        {
            StartDate = new DateOnly(2026, 8, 25),
            EndDate = new DateOnly(2026, 8, 25),
        });

        // A single-day filter is the commonest range there is.
        Assert.True(result.IsValid);
    }

    [Fact]
    public void One_bound_on_its_own_is_valid()
    {
        Assert.True(Subject.Validate(new Parameters { StartDate = new DateOnly(2026, 8, 25) }).IsValid);
        Assert.True(Subject.Validate(new Parameters { EndDate = new DateOnly(2026, 8, 25) }).IsValid);
    }

    [Fact]
    public void An_invalid_sort_order_is_valid_because_the_setter_silently_ignored_it()
    {
        var parameters = new Parameters { SortOrder = "banana" };

        // Documents why there is no SortOrder rule: the setter keeps the previous value, so the getter never
        // returns anything invalid and a validator would always see "asc".
        Assert.Equal("asc", parameters.SortOrder);
        Assert.True(Subject.Validate(parameters).IsValid);
    }
}
