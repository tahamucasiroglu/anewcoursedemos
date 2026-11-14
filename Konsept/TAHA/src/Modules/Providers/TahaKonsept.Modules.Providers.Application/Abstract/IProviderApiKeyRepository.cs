using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Providers.Domain.Entities;

namespace TahaKonsept.Modules.Providers.Application.Abstract;

/// <summary>
/// API anahtarı deposu.
/// </summary>
public interface IProviderApiKeyRepository : IRepository<ProviderApiKey>
{
    /// <summary>Kullanıcı + platform kaydını (izlenen) bulur.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="platform">Platform.</param>
    /// <param name="ct">İptal belirteci.</param>
    Task<ProviderApiKey?> FindAsync(Guid userId, string platform, CancellationToken ct);
}
