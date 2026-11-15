using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Application.Abstract;

/// <summary>
/// Kayıt ayarı: <see cref="IsRegistrationOpenAsync"/> (iç kullanım, yetkisiz), yönetici için <see cref="GetAsync"/> /
/// <see cref="UpdateAsync"/> (<c>admin.access</c> yoksa Forbidden). Ayar sunucuda saklanır; satır yoksa kayıt açıktır.
/// </summary>
public interface IRegistrationSettingsService : IApplicationService
{
    /// <summary>Yeni kayıtlar açık mı (satır yoksa açık).</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Açıksa <c>true</c>.</returns>
    Task<bool> IsRegistrationOpenAsync(CancellationToken ct = default);

    /// <summary>Yönetici: ayarı okur.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Ayar.</returns>
    Task<Result<RegistrationSettingsDto>> GetAsync(CancellationToken ct = default);

    /// <summary>Yönetici: ayarı kaydeder.</summary>
    /// <param name="settings">Yeni ayar.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Kaydedilen ayar.</returns>
    Task<Result<RegistrationSettingsDto>> UpdateAsync(RegistrationSettingsDto settings, CancellationToken ct = default);
}
