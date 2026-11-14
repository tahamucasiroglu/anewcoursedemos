using System.Diagnostics;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Infrastructure.KeyTesting.Base;

/// <summary>
/// Anahtar testi akışı: platforma özgü ücretsiz isteği kur (<see cref="BuildRequest"/>) ya da özel testi çalıştır
/// (<see cref="TestSpecialAsync"/>), süreyi ölç, HTTP durumunu Türkçe sonuca çevir (<see cref="Interpret"/>).
/// </summary>
public abstract class ApiKeyTesterBase : IApiKeyTester
{
    /// <summary>Kurucu.</summary>
    /// <param name="http">HTTP istemcisi.</param>
    protected ApiKeyTesterBase(HttpClient http)
    {
        Http = http ?? throw new ArgumentNullException(nameof(http));
    }

    /// <summary>HTTP istemcisi.</summary>
    protected HttpClient Http { get; }

    /// <inheritdoc />
    public async Task<ApiKeyTestResult> TestAsync(ProviderCredential credential, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(credential);
        var started = Stopwatch.GetTimestamp();
        var name = ProviderPlatformDirectory.DisplayNameOf(credential.Platform);
        try
        {
            int? status = await TestSpecialAsync(credential, ct);
            if (status is null)
            {
                using var request = BuildRequest(credential);
                if (request is null)
                {
                    return new ApiKeyTestResult { Platform = credential.Platform, Success = false, Message = $"{name} için test desteklenmiyor." };
                }

                using var response = await Http.SendAsync(request, ct);
                status = (int)response.StatusCode;
            }

            return Interpret(credential.Platform, name, status.Value, Elapsed(started));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            return new ApiKeyTestResult
            {
                Platform = credential.Platform,
                Success = false,
                Message = $"{name} sunucusuna ulaşılamadı: {ex.Message}",
                DurationMs = Elapsed(started),
            };
        }
    }

    /// <summary>Platforma özgü doğrulama isteğini kurar (desteklenmiyorsa null).</summary>
    /// <param name="credential">Kimlik bilgisi.</param>
    protected abstract HttpRequestMessage? BuildRequest(ProviderCredential credential);

    /// <summary>İsteğe bağlı özel test (ör. SDK ile); HTTP durum kodu döner, uygulanmıyorsa null.</summary>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <param name="ct">İptal belirteci.</param>
    protected virtual Task<int?> TestSpecialAsync(ProviderCredential credential, CancellationToken ct) => Task.FromResult<int?>(null);

    /// <summary>HTTP durumunu sonuca çevirir.</summary>
    /// <param name="platform">Platform.</param>
    /// <param name="name">Görünen ad.</param>
    /// <param name="status">HTTP durum kodu.</param>
    /// <param name="durationMs">Süre.</param>
    protected static ApiKeyTestResult Interpret(string platform, string name, int status, long durationMs)
    {
        var (success, message) = status switch
        {
            >= 200 and < 300 => (true, $"{name} anahtarı geçerli."),
            401 or 403 => (false, $"{name} anahtarı reddedildi (HTTP {status})."),
            429 => (true, $"{name} anahtarı kabul edildi ancak hız sınırına takıldı (HTTP 429)."),
            >= 500 => (false, $"{name} şu an yanıt vermiyor (HTTP {status}); daha sonra tekrar deneyin."),
            _ => (false, $"{name} anahtarı doğrulanamadı (HTTP {status})."),
        };
        return new ApiKeyTestResult { Platform = platform, Success = success, Message = message, StatusCode = status, DurationMs = durationMs };
    }

    /// <summary>Geçen süre (ms).</summary>
    /// <param name="started">Başlangıç zaman damgası.</param>
    private static long Elapsed(long started) => (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
}
