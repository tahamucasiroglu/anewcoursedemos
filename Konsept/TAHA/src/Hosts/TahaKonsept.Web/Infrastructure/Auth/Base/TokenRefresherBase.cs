using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TahaKonsept.Shared.Contracts.Identity;
using TahaKonsept.Web.Infrastructure.Auth.Abstract;

namespace TahaKonsept.Web.Infrastructure.Auth.Base;

/// <summary>
/// <see cref="ITokenRefresher"/> ortak davranışı: tek kapı (SemaphoreSlim) ile aynı anda tek yenileme, sonucun eski
/// refresh token'ın özetiyle 2 dk önbellekte tutulması (paralel istekler aynı yeni çifti kullanır), reddedilen token'ın
/// kısa süre "geçersiz" işaretlenmesi ve sonucun <see cref="ITokenStore"/>'a yazılması.
/// API çağrısı somut sınıfta: <see cref="RequestRefreshAsync"/>.
/// </summary>
public abstract class TokenRefresherBase : ITokenRefresher
{
    private static readonly TimeSpan ReuseWindow = TimeSpan.FromMinutes(2);
    private static readonly object Rejected = new();
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Token deposu.</summary>
    protected ITokenStore Store { get; }

    /// <summary>Yenileme sonuçları önbelleği.</summary>
    protected IMemoryCache Cache { get; }

    /// <summary>Günlükleyici.</summary>
    protected ILogger Logger { get; }

    /// <summary>Bağımlılıkları alır.</summary>
    /// <param name="store">Token deposu.</param>
    /// <param name="cache">Bellek önbelleği.</param>
    /// <param name="logger">Günlükleyici.</param>
    protected TokenRefresherBase(ITokenStore store, IMemoryCache cache, ILogger logger)
    {
        Store = store;
        Cache = cache;
        Logger = logger;
    }

    /// <inheritdoc />
    public async Task<TokenResponse?> RefreshAsync(TokenResponse current, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(current.RefreshToken))
        {
            return null;
        }

        var key = CacheKey(current.RefreshToken);
        if (TryFromCache(key, out var reused))
        {
            return await ApplyAsync(reused);
        }

        await _gate.WaitAsync(ct);
        try
        {
            if (TryFromCache(key, out reused))
            {
                return await ApplyAsync(reused);
            }

            TokenResponse? fresh;
            try
            {
                fresh = await RequestRefreshAsync(current.RefreshToken, ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                Logger.LogWarning(ex, "Token yenilenemedi: API'ye ulaşılamadı. Oturum korunuyor.");
                return null;
            }

            Cache.Set(key, fresh is null ? Rejected : fresh, ReuseWindow);
            if (fresh is null)
            {
                Logger.LogInformation("Refresh token reddedildi; oturum kapatılıyor.");
                await Store.ClearAsync();
                return null;
            }

            await Store.SaveAsync(fresh);
            return fresh;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>API'den yeni token çifti ister. API reddederse null; ağ hatasında istisna fırlatır.</summary>
    /// <param name="refreshToken">Mevcut refresh token.</param>
    /// <param name="ct">İptal belirteci.</param>
    protected abstract Task<TokenResponse?> RequestRefreshAsync(string refreshToken, CancellationToken ct);

    private bool TryFromCache(string key, out TokenResponse? tokens)
    {
        tokens = null;
        if (!Cache.TryGetValue(key, out var value))
        {
            return false;
        }

        tokens = value as TokenResponse;
        return true;
    }

    private async Task<TokenResponse?> ApplyAsync(TokenResponse? tokens)
    {
        if (tokens is null)
        {
            await Store.ClearAsync();
            return null;
        }

        await Store.SaveAsync(tokens);
        return tokens;
    }

    private static string CacheKey(string refreshToken) =>
        "tk-refresh:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
