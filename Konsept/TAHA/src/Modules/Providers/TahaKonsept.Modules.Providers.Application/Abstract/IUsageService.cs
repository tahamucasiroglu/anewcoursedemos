using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Abstract;

/// <summary>
/// Harcama raporları (oturumdaki kullanıcı): bu ayın toplamı + limit + platform kırılımı.
/// </summary>
public interface IUsageService
{
    /// <summary>Bu ayın harcaması.</summary>
    /// <param name="ct">İptal belirteci.</param>
    Task<Result<MonthlyUsageResponse>> GetMonthlyAsync(CancellationToken ct);
}
