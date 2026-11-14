using Konsept.Modules.Monitoring.Contracts.Dtos;

namespace Konsept.Modules.Monitoring.Contracts.Abstract;

/// <summary>
/// Genel olay kaydını okur (Günlük şeridi).
/// </summary>
public interface ISystemEventReader
{
    /// <summary>En son olayı döndürür.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>En son olay; kayıt yoksa <c>null</c>.</returns>
    Task<SystemEventDto?> GetLatestAsync(CancellationToken cancellationToken);
}
