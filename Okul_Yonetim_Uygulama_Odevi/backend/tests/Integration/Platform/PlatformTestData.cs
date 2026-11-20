using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Infrastructure.Security;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using OkulYonetim.WebAPI.Cli;
using Shouldly;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Platform;

/// <summary>
/// Spec 0007 test yardımcıları: sahte saatli API, komutla yetki verme, iki adımlı doğrulama (kurulum + kod) ve testte
/// doğrulayıcı uygulamanın yerine kod üretme (RFC 6238, saat sahte saatten).
/// </summary>
public static class PlatformTestData
{
    public const string Schools = "/api/platform/schools";

    public static FakeTimeProvider Clock() => new(DateTimeOffset.UtcNow);

    public static async Task<(int Exit, string Output)> CliAsync(OkulApiFactory factory, string command, params string[] args)
    {
        using StringWriter output = new();
        int exit = await PlatformAdminCli.RunAsync(factory.Services, command, args, output, TestContext.Current.CancellationToken);
        return (exit, output.ToString());
    }

    /// <summary>Kendi okulu olan (kurucu) bir kişi açar ve ona platform yöneticisi yetkisi verir.</summary>
    public static async Task<SchoolSeed> NewPlatformAdminAsync(OkulApiFactory factory)
    {
        SchoolSeed own = await CreateSchoolAsync(factory, UniqueEmail("platform"));
        (int exit, _) = await CliAsync(factory, "grant-platform-admin", "--eposta", own.FounderEmail);
        exit.ShouldBe(0);
        return own;
    }

    public static string Code(string secret, FakeTimeProvider time, int offsetSteps = 0) =>
        Totp.Code(Totp.FromBase32(secret), (time.GetUtcNow().ToUnixTimeSeconds() / 30) + offsetSteps);

    public static async Task<string> ChallengeAsync(HttpClient client, string email, string password = Password)
    {
        JsonElement login = await LoginAsync(client, email, password);
        login.GetProperty("accessToken").GetString().ShouldBeNullOrEmpty("platform yöneticisine şifreyle oturum açılmaz");
        return login.GetProperty("twoFactorChallenge").GetString()!;
    }

    public static async Task<(HttpStatusCode Status, JsonElement Body, string Raw)> PostAsync(HttpClient client, string path, object body)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(new Uri(path, UriKind.Relative), body, TestContext.Current.CancellationToken);
        string raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        JsonElement json = string.IsNullOrEmpty(raw) ? default : JsonDocument.Parse(raw).RootElement.Clone();
        return (response.StatusCode, json, raw);
    }

    /// <summary>İlk giriş: kurulum anahtarı alınır, kodla kurulum tamamlanır. Dönüş: oturum yanıtı ve anahtar.</summary>
    public static async Task<(JsonElement Auth, string Secret)> SetUpTwoFactorAsync(HttpClient client, FakeTimeProvider time, string email)
    {
        string challenge = await ChallengeAsync(client, email);
        var setup = await PostAsync(client, "/api/auth/two-factor/setup", new { challenge });
        setup.Status.ShouldBe(HttpStatusCode.OK, setup.Raw);
        string secret = setup.Body.GetProperty("secret").GetString()!;
        var confirmed = await PostAsync(client, "/api/auth/two-factor/setup/confirm", new { challenge, code = Code(secret, time), refreshTokenInBody = true });
        confirmed.Status.ShouldBe(HttpStatusCode.OK, confirmed.Raw);
        return (confirmed.Body, secret);
    }

    /// <summary>Kurulumu yapılmış hesapla kodlu giriş (aynı dilim tekrar kabul edilmediği için saat bir dilim ilerletilir).</summary>
    public static async Task<JsonElement> SignInWithCodeAsync(HttpClient client, FakeTimeProvider time, string email, string secret)
    {
        time.Advance(TimeSpan.FromSeconds(30));
        string challenge = await ChallengeAsync(client, email);
        var verified = await PostAsync(client, "/api/auth/two-factor/verify", new { challenge, code = Code(secret, time), refreshTokenInBody = true });
        verified.Status.ShouldBe(HttpStatusCode.OK, verified.Raw);
        return verified.Body;
    }

    /// <summary>Platform yöneticisi açar, kurulumu yapar; panel için erişim anahtarı ve doğrulama anahtarı döner.</summary>
    public static async Task<(SchoolSeed Admin, string Token, string Secret)> SignedInPlatformAdminAsync(OkulApiFactory factory, HttpClient client, FakeTimeProvider time)
    {
        SchoolSeed admin = await NewPlatformAdminAsync(factory);
        (JsonElement auth, string secret) = await SetUpTwoFactorAsync(client, time, admin.FounderEmail);
        return (admin, AccessToken(auth), secret);
    }
}
