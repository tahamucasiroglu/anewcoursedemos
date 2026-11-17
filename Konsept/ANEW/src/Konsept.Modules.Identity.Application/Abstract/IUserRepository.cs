using Konsept.Modules.Identity.Domain.Entities;
using Konsept.Modules.Identity.Domain.Rules;

namespace Konsept.Modules.Identity.Application.Abstract;

/// <summary>Kullanıcıların kalıcılık yüzeyi (uygulaması Infrastructure'da; sayaç işlemleri atomiktir, plan Risk 3).</summary>
public interface IUserRepository
{
    /// <summary>Normalleştirilmiş e-postayla kullanıcı bulur.</summary>
    /// <param name="normalizedEmail">Normalleştirilmiş e-posta.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kullanıcı ya da <c>null</c>.</returns>
    Task<User?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    /// <summary>Kimlikle kullanıcı bulur.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kullanıcı ya da <c>null</c>.</returns>
    Task<User?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Kullanıcıyı ekler; aynı normalleştirilmiş e-posta varsa eklemez (tekil indeks, eşzamanlı kayıt dahil).</summary>
    /// <param name="user">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Eklendiyse <c>true</c>; e-posta zaten kayıtlıysa <c>false</c>.</returns>
    Task<bool> TryAddAsync(User user, CancellationToken cancellationToken);

    /// <summary>
    /// Yanlış denemeyi tek atomik işlemle uygular (<see cref="LockoutPolicy.Evaluate"/>): eşzamanlı denemeler sırayla
    /// sayılır; kilitliyken hiçbir şey değişmez.
    /// </summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="now">Deneme zamanı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Denemeden sonraki durum.</returns>
    Task<LockoutOutcome> RegisterFailedAttemptAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Başarılı girişi atomik uygular (sayaç 0, son giriş zamanı); hesap kilitliyse hiçbir şey değişmez.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="now">Giriş zamanı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Uygulandıysa <c>true</c>.</returns>
    Task<bool> RegisterSuccessfulLoginAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>
    /// Rolleri ayardaki admin e-postasına göre tek atomik işlemle eşitler (spec 0011 Req 5, plan Risk 2): normalleştirilmiş
    /// e-postası eşleşen hesap admin, diğer her hesap kullanıcı olur; <paramref name="normalizedAdminEmail"/> <c>null</c>
    /// ise kimse admin değildir.
    /// </summary>
    /// <param name="normalizedAdminEmail">Ayardaki e-postanın normalleştirilmiş biçimi ya da <c>null</c>.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Rolü değişen hesap sayısı.</returns>
    Task<int> SyncAdminRoleAsync(string? normalizedAdminEmail, CancellationToken cancellationToken);

    /// <summary>Tüm kullanıcıları listeler (spec 0024 admin listesi; tek Pi, az kullanıcı).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kullanıcılar.</returns>
    Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Ban durumunu tek atomik koşullu işlemle değiştirir (spec 0024 Req 3, 6): <paramref name="bannedAt"/> verilirse yalnızca
    /// banlı olmayan hesabı banlar; <c>null</c> ise yalnızca banlı hesabın banını kaldırır. Eşzamanlı iki istekten yalnızca biri
    /// değiştirir (çift tıklama tek kayıt).
    /// </summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="bannedAt">Ban anı ya da kaldırmak için <c>null</c>.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum değiştiyse <c>true</c>.</returns>
    Task<bool> SetBannedAsync(Guid userId, DateTimeOffset? bannedAt, CancellationToken cancellationToken);
}
