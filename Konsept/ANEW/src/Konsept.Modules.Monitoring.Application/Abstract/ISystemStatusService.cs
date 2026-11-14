using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Monitoring.Application.Abstract;

/// <summary>
/// Sistem durumu (site durum alanı) ve sağlık kontrolü.
/// </summary>
public interface ISystemStatusService
{
    /// <summary>Versiyon, veritabanı, ajan ve son olayı döndürür.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sistem durumu.</returns>
    Task<Result<SystemStatusDto>> GetStatusAsync(CancellationToken cancellationToken);

    /// <summary>Kimliksiz sağlık kontrolü bilgisini döndürür.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sağlık bilgisi.</returns>
    Task<Result<HealthDto>> GetHealthAsync(CancellationToken cancellationToken);
}
