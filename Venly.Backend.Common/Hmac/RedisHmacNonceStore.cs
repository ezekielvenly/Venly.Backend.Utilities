using StackExchange.Redis;

namespace Venly.Backend.Common.Hmac;

/// <summary>
/// Replay protection for HMAC-signed internal calls. <see cref="NoOpHmacNonceStore"/> reserves nothing and
/// always returns true, so with it registered a captured request could be replayed verbatim for as long as its
/// timestamp stayed inside the tolerance window — the X-Nonce header was carried, signed, and then ignored.
/// This store makes the reservation real: the first caller to present a nonce wins, every later presentation of
/// the same nonce is rejected.
/// </summary>
public class RedisHmacNonceStore(IConnectionMultiplexer redis) : IHmacNonceStore
{
    public static string KeyFor(string nonce) => $"hmac-nonce:{nonce}";

    public async Task<bool> TryReserveAsync(
        string nonce, TimeSpan ttl, CancellationToken cancellationToken = default) =>
        // SET ... NX is the whole mechanism: a single atomic round trip that both tests for the nonce and
        // claims it. A separate EXISTS-then-SET would let two concurrent replays of one captured request both
        // see "absent" and both proceed. The TTL only needs to outlive the signature's timestamp tolerance —
        // past that the timestamp check rejects the replay on its own, so keeping nonces longer buys nothing.
        await redis.GetDatabase().StringSetAsync(KeyFor(nonce), "1", ttl, When.NotExists);
}
