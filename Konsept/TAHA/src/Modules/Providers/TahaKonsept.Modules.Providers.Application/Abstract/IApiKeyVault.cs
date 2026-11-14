using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Models;

namespace TahaKonsept.Modules.Providers.Application.Abstract;

/// <summary>
/// Şifreli anahtarı çözüp <see cref="ProviderCredential"/> verir (modül içi; görsel/ses gateway'leri de kullanır).
/// Anahtar yoksa <c>providers.key_missing</c> (400) döner. Paralel çağrılara güvenlidir (kendi kapsamını açar).
/// </summary>
public interface IApiKeyVault
{
    /// <summary>Kullanıcının platform anahtarını çözer.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="platform">Platform.</param>
    /// <param name="ct">İptal belirteci.</param>
    Task<Result<ProviderCredential>> GetCredentialAsync(Guid userId, string platform, CancellationToken ct);
}
