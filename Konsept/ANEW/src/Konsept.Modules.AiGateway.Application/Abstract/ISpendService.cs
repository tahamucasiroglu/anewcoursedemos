using Konsept.Modules.AiGateway.Application.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.AiGateway.Application.Abstract;

/// <summary>İsteği yapan kullanıcının aylık harcaması (spec 0013 Req 15–17; yalnızca bilgi).</summary>
public interface ISpendService
{
    /// <summary>Bu ayın (İstanbul) toplamını getirir.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Toplam.</returns>
    Task<Result<MonthlySpendDto>> GetMonthlyAsync(CancellationToken cancellationToken);
}
