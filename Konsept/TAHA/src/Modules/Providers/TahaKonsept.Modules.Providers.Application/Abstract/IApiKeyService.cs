using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Abstract;

/// <summary>
/// Ayarlar → API anahtarları (oturumdaki kullanıcı için): listele (maskeli), kaydet (şifreli), sil, test et.
/// </summary>
public interface IApiKeyService
{
    /// <summary>Tüm online platformlar ve kayıt durumları.</summary>
    /// <param name="ct">İptal belirteci.</param>
    Task<Result<IReadOnlyList<ApiKeyInfo>>> ListAsync(CancellationToken ct);

    /// <summary>Anahtarı ekler / değiştirir.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    Task<Result<ApiKeyInfo>> SaveAsync(SaveApiKeyRequest request, CancellationToken ct);

    /// <summary>Anahtarı siler.</summary>
    /// <param name="platform">Platform.</param>
    /// <param name="ct">İptal belirteci.</param>
    Task<Result> DeleteAsync(string platform, CancellationToken ct);

    /// <summary>Anahtarı ucuz bir çağrıyla doğrular ve sonucu kaydeder.</summary>
    /// <param name="platform">Platform.</param>
    /// <param name="ct">İptal belirteci.</param>
    Task<Result<ApiKeyTestResult>> TestAsync(string platform, CancellationToken ct);
}
