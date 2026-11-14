using System.Net;
using Konsept.Shared.Kernel.Enums;
using Konsept.Shared.Kernel.Security;
using Konsept.Web.ApiClient;
using Konsept.Web.ApiClient.Abstract;
using Konsept.Web.ApiClient.Concrete;
using Konsept.Web.ApiClient.Dtos;
using Konsept.Web.Tests.Support;
using NSubstitute;
using Shouldly;

namespace Konsept.Web.Tests;

/// <summary>
/// Web.ApiClient kimlik çağrıları (plan 0002 adım 5): uçlar, istemci IP'sinin yalnızca servis anahtarıyla iletilmesi,
/// 401/429/alan hatası/kalan hak/kilit eşlemesi ve erişim token'ının Bearer olarak eklenmesi.
/// </summary>
public sealed class ApiClientAuthTests
{
    private const string Session = """{"accessToken":"jwt","accessTokenExpiresAt":"2026-11-14T09:15:00Z","refreshToken":"knr_x","userId":"0195f3a0-0000-7000-8000-000000000001","name":"Ayşe"}""";

    /// <summary>Giriş/kayıt istemci IP'sini servis anahtarıyla iletir; anahtar yoksa IP de iletilmez.</summary>
    /// <param name="key">Servis anahtarı.</param>
    /// <returns>Görev.</returns>
    [Theory]
    [InlineData("svc-key-1")]
    [InlineData(null)]
    public async Task LoginRegister_ForwardClientIp_OnlyWithServiceKey(string? key)
    {
        var handler = new RecordingHandler(Session);
        var client = Create(handler, key);

        var login = await client.LoginAsync(new LoginApiRequest("a@x.com", "Sifre1234"), "192.168.1.20", TestContext.Current.CancellationToken);
        var register = await client.RegisterAsync(new RegisterApiRequest("A", "a@x.com", "Sifre1234"), "192.168.1.20", TestContext.Current.CancellationToken);

        login.Value.Name.ShouldBe("Ayşe");
        register.IsSuccess.ShouldBeTrue();
        handler.Requests.Select(r => r.RequestUri!.AbsolutePath).ShouldBe(["/api/auth/login", "/api/auth/register"]);
        foreach (var request in handler.Requests)
        {
            request.Headers.Contains(ClientIpHeader.Name).ShouldBe(key is not null);
            request.Headers.Contains(ServiceKeyHeader.Name).ShouldBe(key is not null);
        }
    }

    /// <summary>Yanlış şifre 401 + kalan hak; kilit 401 + açılma zamanı; 429 TooManyRequests; 400 alan hataları.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Problems_MappedToErrorTypesAndMetadata()
    {
        var ct = TestContext.Current.CancellationToken;

        var wrong = await Create(new RecordingHandler("""{"title":"Yanlış girdiniz, 2 hakkınız kaldı","code":"identity.wrong_password","remainingAttempts":2}""", HttpStatusCode.Unauthorized))
            .LoginAsync(new LoginApiRequest("a@x.com", "x"), null, ct);
        var locked = await Create(new RecordingHandler("""{"title":"kilit","code":"identity.locked","lockedUntil":"2026-11-14T09:15:00+00:00"}""", HttpStatusCode.Unauthorized))
            .LoginAsync(new LoginApiRequest("a@x.com", "x"), null, ct);
        var many = await Create(new RecordingHandler("""{"title":"Çok fazla deneme","code":"auth.too_many_attempts","correlationId":"c-1"}""", HttpStatusCode.TooManyRequests))
            .LoginAsync(new LoginApiRequest("a@x.com", "x"), null, ct);
        var invalid = await Create(new RecordingHandler("""{"title":"Girilen bilgiler geçersiz.","errors":{"Password":["Şifre 8 ile 128 karakter arasında olmalı."]}}""", HttpStatusCode.BadRequest))
            .RegisterAsync(new RegisterApiRequest("A", "a@x.com", "x"), null, ct);
        var expired = await Create(new RecordingHandler(string.Empty, HttpStatusCode.Unauthorized)).GetSessionAsync("jwt", ct);

        wrong.Error!.Type.ShouldBe(ErrorType.Unauthorized);
        wrong.Error.Metadata![ApiClientErrors.RemainingAttemptsKey].ShouldBe(2);
        locked.Error!.Metadata![ApiClientErrors.LockedUntilKey].ShouldBe(new DateTimeOffset(2026, 11, 14, 9, 15, 0, TimeSpan.Zero));
        many.Error!.Type.ShouldBe(ErrorType.TooManyRequests);
        many.Error.Code.ShouldBe(ApiClientErrors.TooManyAttemptsCode);
        invalid.Error!.Code.ShouldBe(ApiClientErrors.ValidationCode);
        invalid.Error.FieldErrors!["Password"].ShouldBe(["Şifre 8 ile 128 karakter arasında olmalı."]);
        expired.Error!.Type.ShouldBe(ErrorType.Unauthorized);
    }

    /// <summary>
    /// Başarılı durum koduyla bozuk gövde (ör. araya giren vekilin HTML'i) istisna fırlatmaz, "API erişilemiyor" olur (review
    /// R3-3): giriş sayfası çökmek yerine korelasyonlu hatayı gösterir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task MalformedSuccessBody_MapsToUnavailable()
    {
        var result = await Create(new RecordingHandler("<html>vekil</html>")).LoginAsync(new LoginApiRequest("a@x.com", "Sifre1234"), null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe(ApiClientErrors.UnavailableCode);
    }

    /// <summary>Oturum ve çıkış çağrıları verilen token'ı taşır; diğer çağrılara token kaynağından Bearer eklenir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AccessToken_ExplicitOrFromSource_SentAsBearer()
    {
        var ct = TestContext.Current.CancellationToken;
        var source = Substitute.For<IAccessTokenSource>();
        source.GetAccessToken().Returns("from-cookie");
        var recorder = new RecordingHandler("""{"userId":"0195f3a0-0000-7000-8000-000000000001","name":"A","email":"a@x.com"}""");
        using var http = new HttpClient(new AccessTokenHandler(source) { InnerHandler = recorder }) { BaseAddress = new Uri("http://api.test/") };
        var client = new KonseptApiClient(http, new ApiClientOptions(null));

        await client.GetSessionAsync("explicit-token", ct);
        await client.GetSystemStatusAsync(ct);

        recorder.Requests[0].Headers.Authorization!.Parameter.ShouldBe("explicit-token");
        recorder.Requests[1].Headers.Authorization!.Parameter.ShouldBe("from-cookie");
    }

    private static KonseptApiClient Create(RecordingHandler handler, string? key = null) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") }, new ApiClientOptions(key));
}
