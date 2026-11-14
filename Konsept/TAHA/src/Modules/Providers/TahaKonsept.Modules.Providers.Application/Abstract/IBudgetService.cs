using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Abstract;

/// <summary>
/// Ayarlar → Bütçe limiti (oturumdaki kullanıcı): oku / kaydet.
/// </summary>
public interface IBudgetService
{
    /// <summary>Bütçe ayarı (kayıt yoksa varsayılan: limitsiz).</summary>
    /// <param name="ct">İptal belirteci.</param>
    Task<Result<BudgetSettings>> GetAsync(CancellationToken ct);

    /// <summary>Bütçe ayarını kaydeder.</summary>
    /// <param name="settings">Ayar.</param>
    /// <param name="ct">İptal belirteci.</param>
    Task<Result<BudgetSettings>> SaveAsync(BudgetSettings settings, CancellationToken ct);
}
