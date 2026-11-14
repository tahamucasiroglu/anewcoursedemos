using System.Text.Json;
using TahaKonsept.Shared.Contracts.Identity;
using TahaKonsept.Web.Infrastructure.Api;
using TahaKonsept.Web.Infrastructure.Api.Abstract;
using TahaKonsept.Web.Infrastructure.Auth.Abstract;
using TahaKonsept.Web.Models;
using TahaKonsept.Web.Services.Account.Base;
using ApiLoginRequest = TahaKonsept.Shared.Contracts.Identity.LoginRequest;
using ApiSetupRequest = TahaKonsept.Shared.Contracts.Identity.SetupRequest;
using LoginRequest = TahaKonsept.Web.Models.LoginRequest;
using SetupRequest = TahaKonsept.Web.Models.SetupRequest;

namespace TahaKonsept.Web.Services.Account.Concrete;

/// <summary>
/// API Identity uçlarına bağlı oturum servisi:
/// <c>GET api/identity/setup/status</c>, <c>POST api/identity/setup</c>, <c>POST api/identity/login</c>,
/// <c>POST api/identity/logout-all</c>. Kurulum başarılı olunca <see cref="WelcomeUrl"/> (Başlangıç) sayfasına yönlendirir.
/// </summary>
public sealed class AccountService : AccountServiceBase
{
    /// <summary>İlk kurulumdan sonra açılan karşılama sayfası (Başlangıç kontrol listesi).</summary>
    public const string WelcomeUrl = "/Baslangic";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Bağımlılıkları alır.</summary>
    /// <param name="api">API istemcisi.</param>
    /// <param name="tokens">Token deposu.</param>
    public AccountService(IApiClient api, ITokenStore tokens)
        : base(api, tokens)
    {
    }

    /// <inheritdoc />
    public override async Task<bool> IsSetupRequiredAsync(CancellationToken ct = default)
    {
        var result = await Api.GetAsync<SetupStatusResponse>("api/identity/setup/status", ct);
        return result.IsSuccess && result.Data?.SetupRequired == true;
    }

    /// <inheritdoc />
    public override async Task<ApiCallResult<RedirectResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrEmpty(request.Password))
        {
            return Invalid("Kullanıcı adı ve parola gerekli.");
        }

        var result = await Api.PostAsync<ApiLoginRequest, TokenResponse>(
            "api/identity/login",
            new ApiLoginRequest { UserName = request.UserName.Trim(), Password = request.Password },
            ct);

        if (!result.IsSuccess)
        {
            return result.StatusCode is StatusCodes.Status400BadRequest or StatusCodes.Status401Unauthorized
                ? Invalid("Kullanıcı adı ya da parola hatalı.")
                : result.CastFailure<RedirectResponse>();
        }

        return result.Data is { AccessToken.Length: > 0 } tokens
            ? await SignInAsync(tokens, request.RememberMe, request.ReturnUrl)
            : ApiCallResult<RedirectResponse>.Failure(StatusCodes.Status502BadGateway, "API geçerli bir oturum bilgisi döndürmedi.");
    }

    /// <inheritdoc />
    public override async Task<ApiCallResult<RedirectResponse>> SetupAsync(SetupRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || request.UserName.Trim().Length < 3)
        {
            return Invalid("Kullanıcı adı en az 3 karakter olmalı.");
        }

        if (request.Password.Length < 8)
        {
            return Invalid("Parola en az 8 karakter olmalı.");
        }

        if (!string.Equals(request.Password, request.PasswordConfirm, StringComparison.Ordinal))
        {
            return Invalid("Parolalar eşleşmiyor.");
        }

        var result = await Api.PostAsync<ApiSetupRequest, JsonElement>(
            "api/identity/setup",
            new ApiSetupRequest { UserName = request.UserName.Trim(), Password = request.Password },
            ct);

        if (!result.IsSuccess)
        {
            return result.CastFailure<RedirectResponse>();
        }

        var tokens = TryReadTokens(result.Data);
        return tokens is not null
            ? await SignInAsync(tokens, persistent: true, returnUrl: WelcomeUrl)
            : ApiCallResult<RedirectResponse>.Success(new RedirectResponse { RedirectUrl = "/Account/Login?returnUrl=" + Uri.EscapeDataString(WelcomeUrl) });
    }

    /// <inheritdoc />
    public override async Task<ApiCallResult<RedirectResponse>> LogoutAsync(CancellationToken ct = default)
    {
        // API'ye ulaşılamasa bile yerel oturum kapatılır.
        await Api.PostAsync<object, JsonElement>("api/identity/logout-all", new { }, ct);
        await Tokens.ClearAsync();
        return ApiCallResult<RedirectResponse>.Success(new RedirectResponse { RedirectUrl = "/Account/Login" });
    }

    private static TokenResponse? TryReadTokens(JsonElement body)
    {
        if (body.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var tokens = body.Deserialize<TokenResponse>(JsonOptions);
        return tokens is { AccessToken.Length: > 0 } ? tokens : null;
    }
}
