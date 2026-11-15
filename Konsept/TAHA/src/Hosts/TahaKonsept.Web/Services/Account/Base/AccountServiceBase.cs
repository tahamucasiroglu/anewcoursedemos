using TahaKonsept.Shared.Contracts.Identity;
using TahaKonsept.Web.Infrastructure.Api;
using TahaKonsept.Web.Infrastructure.Api.Abstract;
using TahaKonsept.Web.Infrastructure.Auth.Abstract;
using TahaKonsept.Web.Models;
using TahaKonsept.Web.Services.Account.Abstract;
using LoginRequest = TahaKonsept.Web.Models.LoginRequest;
using RegisterRequest = TahaKonsept.Web.Models.RegisterRequest;

namespace TahaKonsept.Web.Services.Account.Base;

/// <summary>
/// <see cref="IAccountService"/> ortak davranışı: token'lardan oturum açma (<see cref="SignInAsync"/>), güvenli yerel
/// dönüş adresi seçimi (<see cref="SafeReturnUrl"/>) ve doğrulama hatası üretimi (<see cref="Invalid"/>).
/// API adresleri somut sınıftadır.
/// </summary>
public abstract class AccountServiceBase : IAccountService
{
    /// <summary>API istemcisi.</summary>
    protected IApiClient Api { get; }

    /// <summary>Token deposu (auth cookie).</summary>
    protected ITokenStore Tokens { get; }

    /// <summary>Bağımlılıkları alır.</summary>
    /// <param name="api">API istemcisi.</param>
    /// <param name="tokens">Token deposu.</param>
    protected AccountServiceBase(IApiClient api, ITokenStore tokens)
    {
        Api = api;
        Tokens = tokens;
    }

    /// <inheritdoc />
    public abstract Task<bool> IsRegistrationOpenAsync(CancellationToken ct = default);

    /// <inheritdoc />
    public abstract Task<ApiCallResult<RedirectResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <inheritdoc />
    public abstract Task<ApiCallResult<RedirectResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    /// <inheritdoc />
    public abstract Task<ApiCallResult<RedirectResponse>> LogoutAsync(CancellationToken ct = default);

    /// <summary>API token'larıyla cookie oturumunu açar ve yönlendirme sonucu döner.</summary>
    /// <param name="tokens">API token'ları.</param>
    /// <param name="persistent">Kalıcı cookie mi.</param>
    /// <param name="returnUrl">Dönülecek adres.</param>
    protected async Task<ApiCallResult<RedirectResponse>> SignInAsync(TokenResponse tokens, bool persistent, string? returnUrl)
    {
        await Tokens.SaveAsync(tokens, persistent);
        return ApiCallResult<RedirectResponse>.Success(new RedirectResponse { RedirectUrl = SafeReturnUrl(returnUrl) });
    }

    /// <summary>Yalnızca bu siteye ait göreli adresleri kabul eder (açık yönlendirme engeli); diğerlerinde "/".</summary>
    /// <param name="returnUrl">İstenen adres.</param>
    protected static string SafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl)
            || !returnUrl.StartsWith('/')
            || returnUrl.StartsWith("//", StringComparison.Ordinal)
            || returnUrl.StartsWith("/\\", StringComparison.Ordinal))
        {
            return "/";
        }

        return returnUrl.StartsWith("/Account", StringComparison.OrdinalIgnoreCase)
            || returnUrl.StartsWith("/Kurulum", StringComparison.OrdinalIgnoreCase) ? "/" : returnUrl;
    }

    /// <summary>400 doğrulama hatası sonucu üretir.</summary>
    /// <param name="message">Kullanıcıya gösterilecek mesaj.</param>
    protected static ApiCallResult<RedirectResponse> Invalid(string message) =>
        ApiCallResult<RedirectResponse>.Failure(StatusCodes.Status400BadRequest, message);
}
