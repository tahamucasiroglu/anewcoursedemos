using TahaKonsept.Modules.Identity.Domain.Entities;

namespace TahaKonsept.Modules.Identity.Application.Abstract;

/// <summary>
/// <see cref="IdentitySetting"/> deposu: <see cref="FindAsync"/> (izlenen), <see cref="TryAddAsync"/> (ekle + kaydet; anahtar zaten
/// varsa — eşzamanlı ekleme dahil — <c>false</c>), <see cref="SaveAsync"/> (izlenen değişiklikleri kaydet).
/// </summary>
public interface IIdentitySettingRepository
{
    /// <summary>Ayarı anahtarıyla bulur (izlenen).</summary>
    /// <param name="key">Anahtar.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Ayar ya da <c>null</c>.</returns>
    Task<IdentitySetting?> FindAsync(string key, CancellationToken ct = default);

    /// <summary>Ayarı ekleyip hemen kaydeder; anahtar zaten varsa hiçbir şey yazılmaz ve <c>false</c> döner.</summary>
    /// <param name="setting">Yeni ayar.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Eklendiyse <c>true</c>.</returns>
    Task<bool> TryAddAsync(IdentitySetting setting, CancellationToken ct = default);

    /// <summary>İzlenen değişiklikleri kaydeder.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task SaveAsync(CancellationToken ct = default);
}
