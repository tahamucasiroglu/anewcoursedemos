using Konsept.Modules.AiGateway.Domain.Entities;

namespace Konsept.Modules.AiGateway.Application.Abstract;

/// <summary>
/// Kullanıcı API key'lerinin kalıcılık yüzeyi. Her işlem kullanıcı kimliğiyle süzülür (BR-1): başka kullanıcının satırı hiçbir
/// yoldan okunmaz, değişmez, silinmez.
/// </summary>
public interface IApiKeyRepository
{
    /// <summary>Kullanıcının key satırlarını getirir.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Satırlar.</returns>
    Task<IReadOnlyList<UserApiKey>> ListAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Kullanıcı + sağlayıcı satırını ekler ya da yerine koyar (aynı anda en fazla bir key).</summary>
    /// <param name="key">Satır.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task UpsertAsync(UserApiKey key, CancellationToken cancellationToken);

    /// <summary>Kullanıcının sağlayıcı key'ini kalıcı siler.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="providerId">Sağlayıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Silinen satır varsa <c>true</c>.</returns>
    Task<bool> DeleteAsync(Guid userId, string providerId, CancellationToken cancellationToken);
}
