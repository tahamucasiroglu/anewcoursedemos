using TahaKonsept.Modules.Production.Domain.Entities;

namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Production'ın işlem günlüğü (Diagnostics <c>IActivityLogger</c>) ve Serilog yazıcısı: aşama başladı/bitti (süre, maliyet), hata, bütçe durdurma, video hazır.
/// </summary>
public interface IProductionActivity
{
    /// <summary>Aşama başladı (sahne bazlı aşamada ilk sahnede).</summary>
    /// <param name="job">İş.</param>
    /// <param name="run">Çalıştırma.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task StageStartedAsync(Job job, StageRun run, CancellationToken cancellationToken);

    /// <summary>Aşama bitti (sahne bazlı aşamada son sahnede, toplam süreyle).</summary>
    /// <param name="job">İş.</param>
    /// <param name="run">Çalıştırma.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task StageFinishedAsync(Job job, StageRun run, CancellationToken cancellationToken);

    /// <summary>Aşama hata verdi.</summary>
    /// <param name="job">İş.</param>
    /// <param name="run">Çalıştırma.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task StageFailedAsync(Job job, StageRun run, CancellationToken cancellationToken);

    /// <summary>Bütçe aşıldığı için sıra durdu (uyarı).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="message">Mesaj.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task BudgetStoppedAsync(Guid userId, string message, CancellationToken cancellationToken);

    /// <summary>Video hazır.</summary>
    /// <param name="job">İş.</param>
    /// <param name="video">Video.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task VideoReadyAsync(Job job, Video video, CancellationToken cancellationToken);

    /// <summary>Genel uyarı satırı (sarı).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="message">Mesaj.</param>
    /// <param name="details">Ayrıntı (JSON'a yazılır; null olabilir).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task WarningAsync(Guid userId, string message, object? details, CancellationToken cancellationToken);

    /// <summary>Genel bilgi satırı.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="message">Mesaj.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task InfoAsync(Guid userId, string message, CancellationToken cancellationToken);
}
