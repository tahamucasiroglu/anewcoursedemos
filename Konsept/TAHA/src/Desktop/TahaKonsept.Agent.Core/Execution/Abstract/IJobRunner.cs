using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Agent.Core.Execution.Abstract;

/// <summary>
/// Sunucudan gelen aşama komutlarını kuyrukta sırayla (varsayılan tek tek) çalıştırır: girdileri indirir, yürütücüye verir,
/// ilerleme/tamamlandı/başarısız raporlarını hub'a iletir. İş başına iptal (<see cref="CancelJob"/>: çalışma alanı silinir) ve
/// duraklatma (<see cref="PauseJob"/>: adım iptal edilir, kontrol noktası dosyaları kalır; aynı işe yeni komut gelince devam).
/// </summary>
public interface IJobRunner
{
    /// <summary>Kuyrukta bekleyen komut sayısı.</summary>
    int QueuedCount { get; }

    /// <summary>Bir aşama ilerlediğinde/başladığında/bittiğinde tetiklenir.</summary>
    event EventHandler<RunningStageInfo>? StageUpdated;

    /// <summary>Komutu kuyruğa ekler (hemen döner).</summary>
    /// <param name="command">Aşama komutu.</param>
    void Submit(StageCommand command);

    /// <summary>İşi iptal eder; çalışan adım durur, kuyruktaki adımları atlanır, çalışma alanı silinir.</summary>
    /// <param name="jobId">İş kimliği.</param>
    void CancelJob(Guid jobId);

    /// <summary>İşi duraklatır; çalışan adım durur, üretilmiş dosyalar korunur.</summary>
    /// <param name="jobId">İş kimliği.</param>
    void PauseJob(Guid jobId);

    /// <summary>Çalışan aşamalar.</summary>
    /// <returns>Liste.</returns>
    IReadOnlyList<RunningStageInfo> GetActiveStages();

    /// <summary>Tüm çalışmayı durdurur (ajan kapanırken); bekleyenler <c>agent.shutdown</c> ile raporlanır.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task StopAsync(CancellationToken ct);
}
