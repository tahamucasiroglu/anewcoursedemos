using Konsept.Modules.AiGateway.Application.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.AiGateway.Application.Abstract;

/// <summary>
/// İsteği yapan kullanıcının API key'leri (spec 0004 Req 7–13): listele, kaydet/değiştir (doğrulamalı), sil. Yanıtlarda tam key
/// asla yoktur.
/// </summary>
public interface IApiKeyService
{
    /// <summary>Katalogdaki her online sağlayıcı için kullanıcının satırını getirir.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Satırlar.</returns>
    Task<Result<IReadOnlyList<ApiKeyStatusDto>>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Key'i doğrular ve kaydeder (varsa eskisinin yerine).</summary>
    /// <param name="providerId">Sağlayıcı.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Yeni satır · 400 biçim/sağlayıcı reddi · 404 bilinmeyen sağlayıcı.</returns>
    Task<Result<ApiKeyStatusDto>> SaveAsync(string providerId, SaveApiKeyRequest? request, CancellationToken cancellationToken);

    /// <summary>Key'i kalıcı siler.</summary>
    /// <param name="providerId">Sağlayıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>"Girilmemiş" satırı · 404 bilinmeyen sağlayıcı.</returns>
    Task<Result<ApiKeyStatusDto>> DeleteAsync(string providerId, CancellationToken cancellationToken);
}
