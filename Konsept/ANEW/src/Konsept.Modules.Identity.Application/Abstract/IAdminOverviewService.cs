using Konsept.Modules.Identity.Application.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Identity.Application.Abstract;

/// <summary>
/// Admin alanının özeti (spec 0011 Req 4): bu spec'te iskelettir. Çağrı yalnızca <c>admin.panel</c> yetkisiyle ulaşır
/// (politika Api'de).
/// </summary>
public interface IAdminOverviewService
{
    /// <summary>Admin özetini döndürür.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Özet (şimdilik boş bölüm listesi).</returns>
    Task<Result<AdminOverviewDto>> GetOverviewAsync(CancellationToken cancellationToken);
}
