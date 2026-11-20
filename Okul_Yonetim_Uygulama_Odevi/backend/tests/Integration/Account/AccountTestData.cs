using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OkulYonetim.Application.Ports;
using OkulYonetim.Tests.Integration.Infrastructure;

namespace OkulYonetim.Tests.Integration.Account;

/// <summary>
/// Spec 0006 test yardımcıları: e-postayı göndermek yerine yakalayan kutu (gönderim arka planda olduğu için testte
/// belirlenimli olsun diye), bağlantı anahtarını e-postadan okuma ve anonim şifre uçlarına çağrı.
/// </summary>
public static partial class AccountTestData
{
    public const string NewPassword = "Yeni-Sifre-2026";
    public const string ResetMessage = "Bu e-posta sistemde kayıtlıysa şifre yenileme bağlantısı gönderildi. Gelen kutunuzu (ve istenmeyen klasörünü) kontrol edin.";

    public static OkulApiFactory Factory(TestDatabase database, CapturingOutbox outbox, TimeProvider? time = null, int rateLimit = 1000) =>
        new(database.ConnectionString, timeProvider: time, loginRateLimitPerMinute: rateLimit, configureServices: services =>
        {
            services.RemoveAll<IEmailOutbox>();
            services.AddSingleton<IEmailOutbox>(outbox);
        });

    public static string TokenFrom(EmailMessage message)
    {
        Match match = LinkToken().Match(message.Body);
        return match.Success ? Uri.UnescapeDataString(match.Groups[1].Value) : throw new InvalidOperationException("e-postada bağlantı yok");
    }

    public static async Task<(HttpStatusCode Status, string Raw)> PostAnonymousAsync(HttpClient client, string path, object body)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(new Uri(path, UriKind.Relative), body, TestContext.Current.CancellationToken);
        // JSON metindeki \uXXXX kaçışları çözülür: Türkçe mesaj doğrudan aranabilsin.
        return (response.StatusCode, Regex.Unescape(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)));
    }

    public static Task<(HttpStatusCode Status, string Raw)> ForgotAsync(HttpClient client, string email) =>
        PostAnonymousAsync(client, "/api/auth/forgot-password", new { email });

    public static Task<(HttpStatusCode Status, string Raw)> ResetAsync(HttpClient client, string token, string password = NewPassword, string? repeat = null) =>
        PostAnonymousAsync(client, "/api/auth/reset-password", new { token, newPassword = password, newPasswordRepeat = repeat ?? password });

    [GeneratedRegex(@"#/sifre-yenile\?anahtar=([^\s]+)")]
    private static partial Regex LinkToken();
}
