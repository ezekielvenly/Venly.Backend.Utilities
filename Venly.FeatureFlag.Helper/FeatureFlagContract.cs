namespace Venly.FeatureFlag.Helper;

/// <summary>
/// Everything a consuming service needs to answer "is this flag on" without another round trip.
///
/// <para>
/// The WHOLE flag set travels, not one flag: a per-key endpoint would put an HTTP call on the hot path of
/// every consumer, and the set is bounded by design — a flag exists because code reads it.
/// </para>
/// <para>
/// This type is the wire contract. AdminService serialises exactly this shape and
/// <see cref="FeatureFlagClient"/> deserialises it, which is why it lives in the shared library rather than
/// being declared twice.
/// </para>
/// </summary>
public sealed record FeatureFlagSnapshot(
    IReadOnlyList<FeatureFlagSnapshotEntry> Flags,
    DateTime GeneratedAt);

/// <summary>
/// One flag: its global state, and the scoped overrides that can beat it.
///
/// Archived flags are NOT included. An archived flag resolves false, and the cheapest way to guarantee that
/// for every consumer is for the snapshot never to mention it.
/// </summary>
public sealed record FeatureFlagSnapshotEntry(
    string Key,
    bool Enabled,
    IReadOnlyList<FeatureFlagOverrideEntry> Overrides);

/// <summary>
/// A scoped override. A null scope component means "any" — <c>CorridorScope: "GB-&gt;NG", Cohort: null</c>
/// applies to that corridor whatever cohort is asked about.
///
/// <paramref name="ExpiresAt"/> of null IS permanent, matching StaffPermissionGrant. There is no sentinel
/// date: a far-future value would render in the console as an expiry and something would have to explain it.
/// </summary>
public sealed record FeatureFlagOverrideEntry(
    string? CorridorScope,
    string? Cohort,
    bool Enabled,
    DateTime? ExpiresAt);

/// <summary>
/// What the caller knows about the request it is deciding for. Both components optional: a service with no
/// corridor in hand asks for the global answer, which is the common case.
/// </summary>
public sealed record FeatureFlagContext(string? Corridor = null, string? Cohort = null);
