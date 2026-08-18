namespace Venly.Backend.Common.Hmac;

public interface IHmacNonceStore
{
    Task<bool> TryReserveAsync(string nonce, TimeSpan ttl, CancellationToken cancellationToken = default);
}

public sealed class NoOpHmacNonceStore : IHmacNonceStore
{
    public Task<bool> TryReserveAsync(string nonce, TimeSpan ttl, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}
