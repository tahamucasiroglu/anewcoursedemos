using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Application.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Identity.Application.Base;

/// <summary>
/// Admin özeti iskeleti: bölümler alt sınıftan gelir; bu spec'te boş liste (0010, 0024, 0025 doldurur). Kullanıcı verisi
/// döndürmez (BR-1).
/// </summary>
public abstract class AdminOverviewServiceBase : IAdminOverviewService
{
    /// <inheritdoc />
    public Task<Result<AdminOverviewDto>> GetOverviewAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Result<AdminOverviewDto>.Success(new AdminOverviewDto(Sections())));

    /// <summary>Admin bölümleri.</summary>
    /// <returns>Bölüm adları.</returns>
    protected abstract IReadOnlyList<string> Sections();
}
