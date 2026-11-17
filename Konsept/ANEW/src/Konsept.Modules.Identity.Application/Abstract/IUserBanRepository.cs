using Konsept.Modules.Identity.Domain.Entities;

namespace Konsept.Modules.Identity.Application.Abstract;

/// <summary>Ban ve ban kaldırma kayıtlarının kalıcılık yüzeyi (spec 0024 Req 8; kayıtlar silinmez).</summary>
public interface IUserBanRepository
{
    /// <summary>Kaydı ekler.</summary>
    /// <param name="record">Kayıt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task AddAsync(UserBanRecord record, CancellationToken cancellationToken);

    /// <summary>Verilen kullanıcıların kayıtlarını en yeni üstte listeler.</summary>
    /// <param name="userIds">Kullanıcılar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıtlar.</returns>
    Task<IReadOnlyList<UserBanRecord>> ListForUsersAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);
}
