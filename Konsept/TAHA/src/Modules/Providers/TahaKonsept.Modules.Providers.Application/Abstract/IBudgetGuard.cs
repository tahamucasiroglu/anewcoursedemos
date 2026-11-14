using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Models;

namespace TahaKonsept.Modules.Providers.Application.Abstract;

/// <summary>
/// GENEL bütçe bekçisi: Concepts/Production yeni bir üretim (iş) başlatmadan önce çağırır. Limit aşılmış ve
/// "aşınca durdur" açıksa <see cref="EnsureCanStartProductionAsync"/> <c>providers.budget_exceeded</c> (409) döner.
/// Devam eden işler engellenmez.
/// </summary>
public interface IBudgetGuard
{
    /// <summary>Bu ayki bütçe durumu.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="ct">İptal belirteci.</param>
    Task<BudgetStatus> GetStatusAsync(Guid userId, CancellationToken ct);

    /// <summary>Yeni üretim başlatılabilir mi; değilse hata.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="ct">İptal belirteci.</param>
    Task<Result> EnsureCanStartProductionAsync(Guid userId, CancellationToken ct);
}
