using System.Text.Json;
using NSubstitute;
using Shouldly;
using TahaKonsept.Shared.Contracts.Identity;
using TahaKonsept.Web.Infrastructure.Api;
using TahaKonsept.Web.Infrastructure.Api.Abstract;
using TahaKonsept.Web.Infrastructure.Auth.Abstract;
using TahaKonsept.Web.Services.Account.Concrete;
using Xunit;
using ApiSetupRequest = TahaKonsept.Shared.Contracts.Identity.SetupRequest;
using SetupRequest = TahaKonsept.Web.Models.SetupRequest;

namespace TahaKonsept.Web.Tests.Services;

/// <summary>
/// İlk kurulum yönlendirmesi: <see cref="AccountService.SetupAsync"/> başarıda Başlangıç sayfasına
/// (<see cref="AccountService.WelcomeUrl"/>) gider; API token vermezse giriş sayfası Başlangıç'a dönecek şekilde açılır.
/// </summary>
public sealed class AccountServiceSetupRedirectTests
{
    private readonly IApiClient _api = Substitute.For<IApiClient>();
    private readonly ITokenStore _tokens = Substitute.For<ITokenStore>();
    private readonly SetupRequest _request = new() { UserName = "taha", Password = "parola123", PasswordConfirm = "parola123" };

    /// <summary>Token dönerse oturum açılır ve /Baslangic'e yönlendirilir.</summary>
    [Fact]
    public async Task Successful_setup_with_tokens_redirects_to_welcome_page()
    {
        SetupReturns("""{"accessToken":"a.b.c","refreshToken":"r","accessTokenExpiresAtUtc":"2030-01-01T00:00:00Z"}""");

        var result = await new AccountService(_api, _tokens).SetupAsync(_request, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Data!.RedirectUrl.ShouldBe("/Baslangic");
        await _tokens.Received(1).SaveAsync(Arg.Any<TokenResponse>(), true);
    }

    /// <summary>Token dönmezse giriş sayfasına, dönüş adresi Başlangıç olarak gidilir.</summary>
    [Fact]
    public async Task Successful_setup_without_tokens_goes_to_login_returning_to_welcome_page()
    {
        SetupReturns("{}");

        var result = await new AccountService(_api, _tokens).SetupAsync(_request, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Data!.RedirectUrl.ShouldBe("/Account/Login?returnUrl=%2FBaslangic");
    }

    private void SetupReturns(string json) =>
        _api.PostAsync<ApiSetupRequest, JsonElement>("api/identity/setup", Arg.Any<ApiSetupRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiCallResult<JsonElement>.Success(JsonDocument.Parse(json).RootElement.Clone()));
}
