using Konsept.Modules.Identity.Contracts.Dtos;

namespace Konsept.Modules.Identity.Contracts.Abstract;

/// <summary>
/// Kullanıcı dizini (spec 0010): toplam kullanıcı, günlük yeni kayıt ve aktivite satırındaki ad/e-posta için. Yalnızca
/// <c>admin.panel</c> korumalı uçlardan çağrılır (yetki kontrolü uçtadır).
/// </summary>
public interface IUserDirectory
{
    /// <summary>Tüm kullanıcıları listeler (tek Pi, az kullanıcı).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kullanıcılar.</returns>
    Task<IReadOnlyList<UserDirectoryEntry>> ListAsync(CancellationToken cancellationToken);
}
