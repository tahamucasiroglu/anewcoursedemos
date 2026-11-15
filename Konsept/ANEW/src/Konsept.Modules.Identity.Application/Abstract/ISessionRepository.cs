using Konsept.Modules.Identity.Domain.Entities;
using Konsept.Modules.Identity.Domain.Enums;

namespace Konsept.Modules.Identity.Application.Abstract;

/// <summary>Oturumların kalıcılık yüzeyi (uygulaması Infrastructure'da; dönüş ve iptal atomiktir).</summary>
public interface ISessionRepository
{
    /// <summary>Oturumu ekler.</summary>
    /// <param name="session">Oturum.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task AddAsync(UserSession session, CancellationToken cancellationToken);

    /// <summary>Oturumu bulur.</summary>
    /// <param name="sessionId">Oturum.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Oturum ya da <c>null</c>.</returns>
    Task<UserSession?> FindAsync(Guid sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Oturumu kullanıcısının o anki rolüyle birlikte tek sorguda bulur (spec 0011, plan Risk 1): her Api isteğinin oturum
    /// doğrulaması rolü de buradan alır, ek gidiş-dönüş olmaz.
    /// </summary>
    /// <param name="sessionId">Oturum.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Oturum ve rol; oturum ya da kullanıcısı yoksa <c>null</c>.</returns>
    Task<(UserSession Session, Role Role)?> FindWithRoleAsync(Guid sessionId, CancellationToken cancellationToken);

    /// <summary>Son kullanım zamanını günceller.</summary>
    /// <param name="sessionId">Oturum.</param>
    /// <param name="now">Kullanım zamanı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task TouchAsync(Guid sessionId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>
    /// Refresh token'ı tek atomik işlemle döndürür: yalnızca hash eşleşen, iptal edilmemiş ve süresi dolmamış oturumda;
    /// son kullanım da güncellenir. Aynı eski token ikinci kez kullanılamaz.
    /// </summary>
    /// <param name="currentHash">Sunulan token'ın hash'i.</param>
    /// <param name="newHash">Yeni token'ın hash'i.</param>
    /// <param name="now">Zaman.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Güncellenen oturum ya da <c>null</c>.</returns>
    Task<UserSession?> RotateAsync(string currentHash, string newHash, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Kullanıcının iptal edilmemiş tüm oturumlarını iptal eder.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="now">İptal zamanı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İptal edilen oturum sayısı.</returns>
    Task<int> RevokeAllAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);
}
