using System.Globalization;
using System.Text.Json;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Infrastructure.Ai;

/// <summary>
/// Yerel AI servislerine tek deneme HTTP çağrısı (spec 0012 AC-9): başarılı yanıtın JSON gövdesini döndürür; ulaşılamayan
/// servis (<c>unreachable</c>), zaman aşımı (<c>timeout</c>), hata durum kodu (<c>http-NNN</c>) ve JSON olmayan yanıt
/// (<c>invalid-response</c>) <see cref="AiUnavailableException"/> olur. Çağıranın iptali iptal olarak kalır. Gövde loglanmaz.
/// </summary>
internal static class AiHttp
{
    public static async Task<JsonElement> PostAsync(HttpClient http, string path, HttpContent content, CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await http.PostAsync(new Uri(path, UriKind.Relative), content, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new AiUnavailableException("http-" + ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture));
            }

            string raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using JsonDocument document = JsonDocument.Parse(raw);
            return document.RootElement.Clone();
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AiUnavailableException("timeout", exception);
        }
        catch (HttpRequestException exception)
        {
            throw new AiUnavailableException("unreachable", exception);
        }
        catch (JsonException exception)
        {
            throw new AiUnavailableException("invalid-response", exception);
        }
    }
}
