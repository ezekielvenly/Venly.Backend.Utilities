namespace Venly.FeatureFlag.Helper;

public interface IFeatureFlagClient
{
    /// <summary>
    /// Whether a flag is on for this context. Never throws: an unknown key, an unreachable AdminService with
    /// no cached snapshot, and a malformed response all answer false.
    /// </summary>
    Task<bool> IsEnabledAsync(string key, FeatureFlagContext? context = null, CancellationToken ct = default);

    /// <summary>The cached snapshot, refreshing it if stale. Null only if one has never been fetched.</summary>
    Task<FeatureFlagSnapshot?> GetSnapshotAsync(CancellationToken ct = default);
}
