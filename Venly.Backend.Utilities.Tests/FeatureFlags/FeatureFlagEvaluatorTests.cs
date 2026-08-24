using Venly.FeatureFlag.Helper;

namespace Venly.Backend.Utilities.Tests.FeatureFlags;

/// <summary>
/// The resolution order is the whole feature. These cases are mirrored verbatim by the console's TypeScript
/// evaluator — if one changes here, the other changes in the same commit.
/// </summary>
public class FeatureFlagEvaluatorTests
{
    private static readonly DateTime Now = new(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc);

    private static FeatureFlagSnapshotEntry Flag(bool enabled, params FeatureFlagOverrideEntry[] overrides) =>
        new("payments.new_rail", enabled, overrides);

    [Fact]
    public void A_missing_flag_is_off()
    {
        Assert.False(FeatureFlagEvaluator.IsEnabled(null, null, Now));
    }

    [Fact]
    public void With_no_overrides_the_global_state_wins()
    {
        Assert.True(FeatureFlagEvaluator.IsEnabled(Flag(true), null, Now));
        Assert.False(FeatureFlagEvaluator.IsEnabled(Flag(false), null, Now));
    }

    [Fact]
    public void A_corridor_override_beats_the_global_state()
    {
        var flag = Flag(false, new FeatureFlagOverrideEntry("GB->NG", null, true, null));

        Assert.True(FeatureFlagEvaluator.IsEnabled(flag, new FeatureFlagContext("GB->NG"), Now));
        Assert.False(FeatureFlagEvaluator.IsEnabled(flag, new FeatureFlagContext("GB->KE"), Now));
        Assert.False(FeatureFlagEvaluator.IsEnabled(flag, null, Now));
    }

    [Fact]
    public void A_corridor_and_cohort_override_beats_a_corridor_only_one()
    {
        var flag = Flag(
            false,
            new FeatureFlagOverrideEntry("GB->NG", null, true, null),
            new FeatureFlagOverrideEntry("GB->NG", "beta", false, null));

        Assert.False(FeatureFlagEvaluator.IsEnabled(
            flag, new FeatureFlagContext("GB->NG", "beta"), Now));
        Assert.True(FeatureFlagEvaluator.IsEnabled(
            flag, new FeatureFlagContext("GB->NG", "general"), Now));
    }

    [Fact]
    public void A_corridor_override_beats_a_cohort_only_one()
    {
        var flag = Flag(
            false,
            new FeatureFlagOverrideEntry(null, "beta", false, null),
            new FeatureFlagOverrideEntry("GB->NG", null, true, null));

        Assert.True(FeatureFlagEvaluator.IsEnabled(flag, new FeatureFlagContext("GB->NG", "beta"), Now));
    }

    [Fact]
    public void An_expired_override_is_ignored_and_the_global_state_stands()
    {
        var flag = Flag(true, new FeatureFlagOverrideEntry("GB->NG", null, false, Now.AddMinutes(-1)));

        Assert.True(FeatureFlagEvaluator.IsEnabled(flag, new FeatureFlagContext("GB->NG"), Now));
    }

    [Fact]
    public void An_override_expiring_in_the_future_still_applies()
    {
        var flag = Flag(true, new FeatureFlagOverrideEntry("GB->NG", null, false, Now.AddMinutes(1)));

        Assert.False(FeatureFlagEvaluator.IsEnabled(flag, new FeatureFlagContext("GB->NG"), Now));
    }

    [Fact]
    public void An_override_with_no_scope_at_all_loses_to_any_scoped_override()
    {
        var flag = Flag(
            true,
            new FeatureFlagOverrideEntry(null, null, false, null),
            new FeatureFlagOverrideEntry(null, "beta", true, null));

        Assert.True(FeatureFlagEvaluator.IsEnabled(flag, new FeatureFlagContext(null, "beta"), Now));
        Assert.False(FeatureFlagEvaluator.IsEnabled(flag, new FeatureFlagContext(null, "general"), Now));
    }

    [Fact]
    public void Scope_matching_is_case_insensitive()
    {
        var flag = Flag(false, new FeatureFlagOverrideEntry("GB->NG", null, true, null));

        Assert.True(FeatureFlagEvaluator.IsEnabled(flag, new FeatureFlagContext("gb->ng"), Now));
    }
}
