using Konsept.Modules.Monitoring.Domain.Entities;

namespace Konsept.Modules.Monitoring.Application.Abstract;

/// <summary>Teknik log kaydının kalıcılık yüzeyi (uygulaması Infrastructure'da).</summary>
public interface ITechnicalLogRepository
{
    /// <summary>Kayıtları ekler.</summary>
    /// <param name="logs">Kayıtlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task AddRangeAsync(IReadOnlyList<TechnicalLog> logs, CancellationToken cancellationToken);

    /// <summary>Verilen zamandan eski kayıtları siler.</summary>
    /// <param name="cutoff">Bu zamandan önceki kayıtlar silinir.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Silinen kayıt sayısı.</returns>
    Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken);
}
