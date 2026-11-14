using Konsept.Modules.Monitoring.Contracts.Dtos;

namespace Konsept.Modules.Monitoring.Contracts.Abstract;

/// <summary>
/// Diğer modüllerin genel olay kaydına olay yazdığı tek yol.
/// </summary>
public interface ISystemEventWriter
{
    /// <summary>Olayı kaydeder.</summary>
    /// <param name="entry">Yeni olay.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kaydedilen olay.</returns>
    Task<SystemEventDto> WriteAsync(SystemEventEntry entry, CancellationToken cancellationToken);
}
