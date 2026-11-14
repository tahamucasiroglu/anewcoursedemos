using Konsept.Shared.Kernel.Results;
using Konsept.Web.ApiClient.Dtos;
using Konsept.Web.Auth.Base;
using Microsoft.Extensions.Caching.Memory;

namespace Konsept.Web.Auth.Concrete;

/// <summary>Tek uçuşlu refresh; paylaşılan sonuçlar <see cref="IMemoryCache"/>'te (Web tek süreç, Pi).</summary>
/// <param name="cache">Bellek önbelleği.</param>
public sealed class TokenRefreshCoordinator(IMemoryCache cache) : TokenRefreshCoordinatorBase
{
    private const string Prefix = "refresh:";

    /// <inheritdoc />
    protected override bool TryGetShared(string key, out Result<AuthSessionResponse>? result) =>
        cache.TryGetValue(Prefix + key, out result);

    /// <inheritdoc />
    protected override void Share(string key, Result<AuthSessionResponse> result) =>
        cache.Set(Prefix + key, result, ShareDuration);
}
