namespace Venly.FeatureFlag.Helper;

/// <summary>
/// The one implementation of what a flag resolves to. AdminService uses it to show an operator what they are
/// about to do; every consumer uses it to decide. Two implementations would eventually disagree, and the
/// disagreement would surface as a console that says a flag is on for a corridor where it is off.
///
/// <para>
/// <b>Most specific wins.</b> An override matches when each of its scope components is either null ("any") or
/// equal to what the caller asked about. Among the matches, the one naming the most components wins: corridor
/// and cohort (3) beats corridor (2) beats cohort (1) beats the flag's global state (0).
/// </para>
/// <para>
/// <b>Absent is off.</b> A null entry — an unknown key, or an archived flag the snapshot omits — resolves
/// false rather than throwing. A consumer asking about a flag nobody declared is asking about a feature that
/// does not exist, and the safe reading of that is "not on".
/// </para>
/// </summary>
public static class FeatureFlagEvaluator
{
    public static bool IsEnabled(
        FeatureFlagSnapshotEntry? flag, FeatureFlagContext? context, DateTime utcNow)
    {
        if (flag is null)
            return false;

        FeatureFlagOverrideEntry? winner = null;
        var best = -1;

        foreach (var candidate in flag.Overrides)
        {
            // An expiry in the past is not a match at all, so the next-most-specific override — or the
            // global state — takes over the moment it lapses, with nothing to sweep up.
            if (candidate.ExpiresAt is { } expiry && expiry <= utcNow)
                continue;

            if (!Matches(candidate.CorridorScope, context?.Corridor))
                continue;

            if (!Matches(candidate.Cohort, context?.Cohort))
                continue;

            var score = (candidate.CorridorScope is null ? 0 : 2) + (candidate.Cohort is null ? 0 : 1);

            // Strictly greater, so a tie keeps the first. Ties can only happen between two overrides of the
            // same shape, which the unique index on (FlagId, Corridor, Cohort) prevents from existing.
            if (score <= best)
                continue;

            best = score;
            winner = candidate;
        }

        return winner?.Enabled ?? flag.Enabled;
    }

    /// <summary>
    /// A null scope component on the OVERRIDE means "any" and always matches. A null on the CONTEXT means the
    /// caller does not know, and only an "any" override can match it — asking without a corridor must never
    /// pick up a corridor-specific rule.
    /// </summary>
    private static bool Matches(string? overrideScope, string? contextScope) =>
        overrideScope is null || string.Equals(overrideScope, contextScope, StringComparison.OrdinalIgnoreCase);
}
