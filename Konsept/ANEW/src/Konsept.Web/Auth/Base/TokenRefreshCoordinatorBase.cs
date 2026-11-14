using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Konsept.Shared.Kernel.Enums;
using Konsept.Shared.Kernel.Results;
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

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public async Task<Result<AuthSessionResponse>> RefreshAsync(IKonseptApiClient api, string refreshToken, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(api);
        ArgumentException.ThrowIfNullOrEmpty(refreshToken);
        var key = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
        if (TryGetShared(key, out var shared))
        {
            return shared!;
        }

        var gate = _gates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (TryGetShared(key, out shared))
            {
                return shared!;
            }

            var result = await api.RefreshAsync(refreshToken, cancellationToken).ConfigureAwait(false);
            if (result.IsSuccess || result.Error!.Type == ErrorType.Unauthorized)
            {
                Share(key, result);
            }

            return result;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>Paylaşılan sonucu okur.</summary>
    /// <param name="key">Eski token'ın hash'i.</param>
    /// <param name="result">Sonuç.</param>
    /// <returns>Varsa <c>true</c>.</returns>
    protected abstract bool TryGetShared(string key, out Result<AuthSessionResponse>? result);

    /// <summary>Sonucu <see cref="ShareDuration"/> boyunca paylaşır.</summary>
    /// <param name="key">Eski token'ın hash'i.</param>
    /// <param name="result">Sonuç.</param>
    protected abstract void Share(string key, Result<AuthSessionResponse> result);
}
