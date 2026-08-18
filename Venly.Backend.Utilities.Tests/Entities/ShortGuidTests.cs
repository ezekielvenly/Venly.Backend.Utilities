using Venly.Backend.Common.Entities;

namespace Venly.Backend.Utilities.Tests.Entities;

public class ShortGuidTests
{
    [Fact]
    public void NewGuid_starts_with_uppercase_seed_and_is_22_characters()
    {
        var id = ShortGuid.NewGuid("tpl");

        Assert.StartsWith("TPL", id);
        Assert.Equal(22, id.Length);
    }

    [Fact]
    public void NewGuid_rejects_seed_that_is_not_three_characters()
    {
        Assert.Throws<ArgumentException>(() => ShortGuid.NewGuid("ab"));
    }

    [Fact]
    public void NewGuid_produces_unique_values()
    {
        var first = ShortGuid.NewGuid("NTF");
        var second = ShortGuid.NewGuid("NTF");

        Assert.NotEqual(first, second);
    }
}
