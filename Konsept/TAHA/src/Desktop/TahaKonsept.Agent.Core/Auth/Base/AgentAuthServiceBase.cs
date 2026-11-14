using TahaKonsept.Agent.Core.Auth.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Agent.Core.Auth.Base;

/// <summary>
/// Oturum servisi tabanı: token'ları bellekte ve <see cref="ITokenStore"/>'da tutar; yenilemeyi <see cref="SemaphoreSlim"/> ile
/// tek uçuşa indirger. Sunucu yenilemeyi <see cref="ErrorType.Unauthorized"/>/<see cref="ErrorType.Validation"/> ile reddederse oturum kapanır;
/// ağ hatasında token'lar korunur. Yenileme çağrısı <see cref="RequestRefreshAsync"/> ile türetilen sınıftadır.
/// </summary>
public abstract class AgentAuthServiceBase : IAgentAuthService
{
    private readonly ITokenStore _store;
    private readonly TimeSpan _refreshSkew;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private TokenResponse? _tokens;

    /// <summary>Oluşturur ve kayıtlı token'ları yükler.</summary>
    /// <param name="store">Token deposu.</param>
    /// <param name="refreshSkew">Bitişe bu süreden az kalınca önceden yenile.</param>
    /// <param name="timeProvider">Zaman sağlayıcı.</param>
    protected AgentAuthServiceBase(ITokenStore store, TimeSpan refreshSkew, TimeProvider timeProvider)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _refreshSkew = refreshSkew;
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _tokens = store.Load();
    }

    /// <inheritdoc />
    public event EventHandler? AuthorizationChanged;

    /// <summary>Zaman sağlayıcı.</summary>
    protected TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    public bool IsPaired
    {
        get
        {
            var tokens = Volatile.Read(ref _tokens);
            return tokens is not null
                && !string.IsNullOrEmpty(tokens.RefreshToken)
                && (tokens.RefreshTokenExpiresAtUtc == default || tokens.RefreshTokenExpiresAtUtc > TimeProvider.GetUtcNow());
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(CancellationToken ct)
    {
        var tokens = Volatile.Read(ref _tokens);
        if (tokens is null)
        {
            return null;
        }

        if (!NeedsRefresh(tokens))
        {
            return tokens.AccessToken;
        }

        await RefreshAsync(ct);
        return Volatile.Read(ref _tokens)?.AccessToken;
    }

    /// <inheritdoc />
    public async Task<bool> RefreshAsync(CancellationToken ct)
    {
        var before = Volatile.Read(ref _tokens);
        if (before is null || string.IsNullOrEmpty(before.RefreshToken))
        {
            return false;
        }

        await _refreshLock.WaitAsync(ct);
        try
        {
            var current = Volatile.Read(ref _tokens);
            if (current is null)
            {
                return false;
            }

            if (!ReferenceEquals(current, before) && !NeedsRefresh(current))
            {
                return true;
            }

            var result = await RequestRefreshAsync(current.RefreshToken, ct);
            if (result.IsSuccess && !string.IsNullOrEmpty(result.Value.AccessToken))
            {
                StoreTokens(result.Value);
                return true;
            }

            if (result.IsFailure && result.Error.Type is ErrorType.Unauthorized or ErrorType.Validation or ErrorType.Forbidden)
            {
                OnRefreshRejected(result.Error);
                SignOut();
            }
            else if (result.IsFailure)
            {
                OnRefreshFailed(result.Error);
            }

            return false;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <inheritdoc />
    public void SetTokens(TokenResponse tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        StoreTokens(tokens);
        AuthorizationChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void SignOut()
    {
        var had = Interlocked.Exchange(ref _tokens, null) is not null;
        _store.Clear();
        if (had)
        {
            AuthorizationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Sunucuya yenileme isteği gönderir.</summary>
    /// <param name="refreshToken">Yenileme token'ı.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Yeni token çifti ya da hata.</returns>
    protected abstract Task<Result<TokenResponse>> RequestRefreshAsync(string refreshToken, CancellationToken ct);

    /// <summary>Sunucu yenilemeyi reddettiğinde çağrılır (oturum kapanmadan önce).</summary>
    /// <param name="error">Hata.</param>
    protected virtual void OnRefreshRejected(Error error)
    {
    }

    /// <summary>Yenileme geçici bir hatayla başarısız olduğunda çağrılır.</summary>
    /// <param name="error">Hata.</param>
    protected virtual void OnRefreshFailed(Error error)
    {
    }

    private bool NeedsRefresh(TokenResponse tokens) =>
        string.IsNullOrEmpty(tokens.AccessToken)
        || (tokens.AccessTokenExpiresAtUtc != default && tokens.AccessTokenExpiresAtUtc - _refreshSkew <= TimeProvider.GetUtcNow());

    private void StoreTokens(TokenResponse tokens)
    {
        _store.Save(tokens);
        Volatile.Write(ref _tokens, tokens);
    }
}
