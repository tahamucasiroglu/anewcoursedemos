using TahaKonsept.BuildingBlocks.Application.Abstract;

namespace TahaKonsept.Modules.Identity.Application.Abstract;

/// <summary>
/// Identity modülünün iş birimi (Identity DbContext). Genel <see cref="IUnitOfWork"/>'ten ayrı tutulur ki
/// diğer modüllerin DbContext kayıtlarıyla çakışmasın. <see cref="TrySaveChangesAsync"/> iyimser eşzamanlılık çakışmasını
/// (ör. aynı refresh token'ın eşzamanlı kullanımı) istisna yerine <c>false</c> olarak bildirir.
/// </summary>
public interface IIdentityUnitOfWork : IUnitOfWork
{
    /// <summary>Kaydeder; eşzamanlılık çakışmasında hiçbir şey yazılmaz, izlenen değişiklikler atılır ve <c>false</c> döner.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kaydedildiyse <c>true</c>.</returns>
    Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken = default);
}
