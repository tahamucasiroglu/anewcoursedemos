using Konsept.Modules.Monitoring.Domain.Entities;

namespace Konsept.Modules.Monitoring.Application.Abstract;

/// <summary>
/// Olay kaydının kalıcılık yüzeyi (uygulaması Infrastructure'da).
/// </summary>
public interface ISystemEventRepository
{
    /// <summary>Olayı kalıcı olarak ekler.</summary>
    /// <param name="systemEvent">Olay.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task AddAsync(SystemEvent systemEvent, CancellationToken cancellationToken);

    /// <summary>En son olayı getirir.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>En son olay ya da <c>null</c>.</returns>
    Task<SystemEvent?> GetLatestAsync(CancellationToken cancellationToken);
}
