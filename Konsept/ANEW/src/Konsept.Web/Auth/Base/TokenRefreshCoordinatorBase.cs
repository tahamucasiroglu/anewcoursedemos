using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Konsept.Shared.Kernel.Enums;
using Konsept.Shared.Kernel.Results;
using Konsept.Web.ApiClient;
using Konsept.Web.ApiClient.Abstract;
using Konsept.Web.ApiClient.Dtos;
using Konsept.Web.Auth.Abstract;

namespace Konsept.Web.Auth.Base;

/// <summary>
/// Tek uçuşlu refresh: eski token'ın hash'i başına bir kilit; kilidi alan Api'yi çağırır, sonucu (başarı ya da kesin red)
/// <see cref="ShareDuration"/> boyunca saklar; bekleyenler aynı sonucu alır. Geçici hata (Api erişilemiyor) saklanmaz.
/// Token düz metin anahtar olarak tutulmaz. Sonuç deposu alt sınıftadır.
/// </summary>
public abstract class TokenRefreshCoordinatorBase : ITokenRefreshCoordinator
{
    /// <summary>Sonucun paylaşıldığı süre.</summary>
    public static readonly TimeSpan ShareDuration = TimeSpan.FromSeconds(60);

    /// <summary>Liderin Api refresh çağrısının üst süresi (isteğin iptalinden bağımsız).</summary>
    public static readonly TimeSpan RefreshTimeout = TimeSpan.FromSeconds(15);

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public async Task<Result<AuthSessionResponse>> RefreshAsync(IKonseptApiClient api, string refreshToken, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(api);
        ArgumentException.ThrowIfNullOrEmpty(refreshToken);
        var key = KeyOf(refreshToken);
        if (TryGetShared(key, out var shared))
        {
            return shared!;
        }

        var gate = _gates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        var resolved = false;
        try
        {
            if (TryGetShared(key, out shared))
            {
                resolved = true;
                return shared!;
            }

            // Api tarafında dönüş bir kez olur: tarayıcı isteği kesilse de lider çağrıyı bitirip sonucu paylaşır, yoksa yeni çift
            // kaybolur ve kullanıcı düşer (review R1-1). Yalnızca zaman aşımıyla iptal edilir; bekleyenler kendi isteğiyle çıkabilir.
            using var detached = new CancellationTokenSource(RefreshTimeout, Time);
            Result<AuthSessionResponse> result;
            try
            {
                result = await api.RefreshAsync(refreshToken, detached.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (detached.IsCancellationRequested)
            {
                // Kendi zaman aşımımız HttpClient'ınkinden önce dolabilir; istemci bu iptali "çağıranın iptali" sanıp istisnayı
                // geçirir (review R8-1). Geçici hatadır: "API erişilemiyor", paylaşılmaz, kilit kalır (R2-1) — sayfa oturumu
                // korur (R2-2/R3-2), çıkış 503 verir (R3-1).
                return Result<AuthSessionResponse>.Failure(ApiClientErrors.Unavailable);
            }

            if (result.IsSuccess || result.Error!.Type == ErrorType.Unauthorized)
            {
                // Başarı kazanır (review R2-1): paylaşılmış bir başarının üstüne 401 yazılmaz.
                if (result.IsSuccess || !TryGetShared(key, out var existing) || existing is not { IsSuccess: true })
                {
                    Share(key, result);
                }

                resolved = true;
            }

            return result;
        }
        finally
        {
            gate.Release();

            // Kilit yalnızca kesin sonuç paylaşıldıktan sonra bırakılır (review R1-3, R2-1): bekleyenler ve sonradan gelenler
            // önbellekten okur. Geçici hatada kilit kalır; böylece aynı eski token için ikinci bir eşzamanlı dönüş başlamaz.
            if (resolved)
            {
                _gates.TryRemove(new KeyValuePair<string, SemaphoreSlim>(key, gate));
            }
        }
    }

    /// <inheritdoc />
    public bool TryGetRotated(string refreshToken, out AuthSessionResponse? session)
    {
        ArgumentException.ThrowIfNullOrEmpty(refreshToken);
        session = TryGetShared(KeyOf(refreshToken), out var shared) && shared is { IsSuccess: true } ? shared.Value : null;
        return session is not null;
    }

    /// <summary>Gets tutulan refresh kilidi sayısı (kesin sonuçla biten uçuşların kilitleri tutulmaz; geçici hatada kilit kalır).</summary>
    public int ActiveGateCount => _gates.Count;

    /// <summary>Gets refresh zaman aşımının saat kaynağı.</summary>
    protected abstract TimeProvider Time { get; }

    /// <summary>Paylaşılan sonucu okur.</summary>
    /// <param name="key">Eski token'ın hash'i.</param>
    /// <param name="result">Sonuç.</param>
    /// <returns>Varsa <c>true</c>.</returns>
    protected abstract bool TryGetShared(string key, out Result<AuthSessionResponse>? result);

    /// <summary>Sonucu <see cref="ShareDuration"/> boyunca paylaşır.</summary>
    /// <param name="key">Eski token'ın hash'i.</param>
    /// <param name="result">Sonuç.</param>
    protected abstract void Share(string key, Result<AuthSessionResponse> result);

    private static string KeyOf(string refreshToken) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
