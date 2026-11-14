using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Abstract;

/// <summary>
/// Bir anahtarı sağlayıcıya ücretsiz/ucuz bir istekle (model listesi, hesap bilgisi) doğrular.
/// </summary>
public interface IApiKeyTester
{
    /// <summary>Anahtarı dener.</summary>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <param name="ct">İptal belirteci.</param>
    Task<ApiKeyTestResult> TestAsync(ProviderCredential credential, CancellationToken ct);
}
