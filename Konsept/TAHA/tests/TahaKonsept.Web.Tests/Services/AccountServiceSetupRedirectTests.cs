using System.Text.Json;
using NSubstitute;
using Shouldly;
using TahaKonsept.Shared.Contracts.Identity;
using TahaKonsept.Web.Infrastructure.Api;
using TahaKonsept.Web.Infrastructure.Api.Abstract;
using TahaKonsept.Web.Infrastructure.Auth.Abstract;
using TahaKonsept.Web.Services.Account.Concrete;
using Xunit;
using ApiRegisterRequest = TahaKonsept.Shared.Contracts.Identity.RegisterRequest;
using RegisterRequest = TahaKonsept.Web.Models.RegisterRequest;

namespace TahaKonsept.Web.Tests.Services;

/// <summary>
/// Kayıt yönlendirmesi ve doğrulaması: <see cref="AccountService.RegisterAsync"/> başarıda Başlangıç sayfasına
/// (<see cref="AccountService.WelcomeUrl"/>) gider; API token vermezse giriş sayfası Başlangıç'a dönecek şekilde açılır; parolalar
/// eşleşmezse API çağrılmaz; kayıt kapalıyken API'nin 403'ü aynen aktarılır; kayıt durumu API'den okunur.
/// </summary>
public sealed class AccountServiceSetupRedirectTests
{
    private readonly IApiClient _api = Substitute.For<IApiClient>();
    private readonly ITokenStore _tokens = Substitute.For<ITokenStore>();
    private readonly RegisterRequest _request = new() { UserName = "taha", Password = "parola123", PasswordConfirm = "parola123" };

    /// <summary>Token dönerse oturum açılır ve /Baslangic'e yönlendirilir.</summary>
    [Fact]
    public async Task Successful_register_with_tokens_redirects_to_welcome_page()
    {
        RegisterReturns("""{"accessToken":"a.b.c","refreshToken":"r","accessTokenExpiresAtUtc":"2030-01-01T00:00:00Z"}""");

        var result = await new AccountService(_api, _tokens).RegisterAsync(_request, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Data!.RedirectUrl.ShouldBe("/Baslangic");
        await _tokens.Received(1).SaveAsync(Arg.Any<TokenResponse>(), true);
    }

    /// <summary>Token dönmezse giriş sayfasına, dönüş adresi Başlangıç olarak gidilir.</summary>
    [Fact]
    public async Task Successful_register_without_tokens_goes_to_login_returning_to_welcome_page()
    {
        RegisterReturns("{}");

        var result = await new AccountService(_api, _tokens).RegisterAsync(_request, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Data!.RedirectUrl.ShouldBe("/Account/Login?returnUrl=%2FBaslangic");
    }

    /// <summary>Parolalar eşleşmezse 400 ve API çağrısı yok.</summary>
    [Fact]
    public async Task Mismatched_passwords_are_rejected_locally()
    {
        var result = await new AccountService(_api, _tokens).RegisterAsync(_request with { PasswordConfirm = "baska123" }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.StatusCode.ShouldBe(400);
        await _api.DidNotReceiveWithAnyArgs().PostAsync<ApiRegisterRequest, JsonElement>(default!, default!, TestContext.Current.CancellationToken);
    }

    /// <summary>Kayıt kapalıyken API'nin 403 cevabı tarayıcıya aktarılır, oturum açılmaz.</summary>
    [Fact]
    public async Task Closed_registration_passes_through_forbidden()
    {
        _api.PostAsync<ApiRegisterRequest, JsonElement>("api/identity/register", Arg.Any<ApiRegisterRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiCallResult<JsonElement>.Failure(403, "Yeni kayıtlar şu an kapalı."));

        var result = await new AccountService(_api, _tokens).RegisterAsync(_request, TestContext.Current.CancellationToken);

        result.StatusCode.ShouldBe(403);
        await _tokens.DidNotReceiveWithAnyArgs().SaveAsync(default!, default);
    }

    /// <summary>Kayıt durumu API'den okunur; API'ye ulaşılamazsa açık sayılır (API zaten reddeder).</summary>
    /// <param name="open">API'nin döndürdüğü durum (null: hata).</param>
    /// <param name="expected">Beklenen.</param>
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(null, true)]
    public async Task Registration_status_is_read_from_api(bool? open, bool expected)
    {
        _api.GetAsync<RegistrationStatusResponse>("api/identity/registration/status", Arg.Any<CancellationToken>())
            .Returns(open is { } value
                ? ApiCallResult<RegistrationStatusResponse>.Success(new RegistrationStatusResponse { RegistrationOpen = value })
                : ApiCallResult<RegistrationStatusResponse>.Failure(502, "yok"));

        (await new AccountService(_api, _tokens).IsRegistrationOpenAsync(TestContext.Current.CancellationToken)).ShouldBe(expected);
    }

    private void RegisterReturns(string json) =>
        _api.PostAsync<ApiRegisterRequest, JsonElement>("api/identity/register", Arg.Any<ApiRegisterRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiCallResult<JsonElement>.Success(JsonDocument.Parse(json).RootElement.Clone()));
}
