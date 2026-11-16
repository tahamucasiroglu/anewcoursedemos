using System.Net;
using System.Net.Http.Headers;
using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Domain.Entities;
using Konsept.Modules.AiGateway.Domain.Enums;

namespace Konsept.Modules.AiGateway.Infrastructure.Validation;

/// <summary>
/// Key'i sağlayıcının seed'deki maliyetsiz GET ucunda (model listesi / key bilgisi) doğrular (spec 0004 Req 10): tek istek,
/// yeniden deneme yok, en fazla seed'deki süre (10 sn). 2xx → kabul, 401 → red, diğer her şey (zaman aşımı, 429, 5xx, bağlantı
/// hatası) ya da ucu olmayan sağlayıcı → sonuçsuz. Token harcayan uçlar (mesaj, tamamlama, görsel) hiç çağrılmaz. HTTP istemcisinin
/// kendi logu kapalıdır; key yalnızca istek başlığındadır.
/// </summary>
/// <param name="http">HTTP istemci fabrikası.</param>
/// <param name="catalog">Katalog (doğrulama süresi).</param>
public sealed class HttpProviderKeyValidator(IHttpClientFactory http, ICatalogSource catalog) : IProviderKeyValidator
{
    /// <summary>Adlandırılmış HTTP istemcisi.</summary>
    public const string ClientName = "ai-key-validation";

    private const string AnthropicVersion = "2023-06-01";

    /// <inheritdoc />
    public async Task<KeyCheckOutcome> CheckAsync(ProviderEntry provider, string key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(provider);
        if (provider.ValidationUrl is null)
        {
            return KeyCheckOutcome.Inconclusive;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(catalog.Catalog.KeyValidationTimeoutSeconds));
        using var request = new HttpRequestMessage(HttpMethod.Get, provider.ValidationUrl);
        if (provider.AuthStyle == ProviderAuthStyle.Anthropic)
        {
            request.Headers.TryAddWithoutValidation("x-api-key", key);
            request.Headers.TryAddWithoutValidation("anthropic-version", AnthropicVersion);
        }
        else
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        }

        try
        {
            using var client = http.CreateClient(ClientName);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return KeyCheckOutcome.Accepted;
            }

            return response.StatusCode == HttpStatusCode.Unauthorized ? KeyCheckOutcome.Rejected : KeyCheckOutcome.Inconclusive;
        }
        catch (HttpRequestException)
        {
            return KeyCheckOutcome.Inconclusive;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return KeyCheckOutcome.Inconclusive;
        }
    }
}
