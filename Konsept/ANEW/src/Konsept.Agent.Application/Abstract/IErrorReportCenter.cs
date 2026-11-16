using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;

namespace Konsept.Agent.Application.Abstract;

/// <summary>
/// Ajandaki "Bir hata oldu, bildir" mekanizması (spec 0022 Req 4–9, 15): hata uyarısı (aynı özet ajan oturumunda bir kez), "Bildir"
/// (bağlıysa gönderir, değilse bekletir), "Hata raporunu dosyaya kaydet", bekleyen bildirimlerin bağlantı + eşleşme kurulunca tam bir kez
/// gönderilmesi, hesap bağı ve 20'lik kuyruk. 0009 (kurulum) ve 0023 (güncelleme) bileşen ve adımla <see cref="Raise"/> çağırır.
/// </summary>
public interface IErrorReportCenter
{
    /// <summary>Yeni bir hata uyarısı gösterilmeli (arka plan iş parçacığından).</summary>
    event EventHandler<AgentErrorReport>? AlertRaised;

    /// <summary>Bekleyen bildirim sayısı değişti.</summary>
    event EventHandler<int>? PendingChanged;

    /// <summary>Bekleyen bir bildirim sonradan gönderildi ya da gönderilemedi (başka hesap, sınır).</summary>
    event EventHandler<ReportNotice>? Notice;

    /// <summary>Gets bekleyen bildirim sayısı.</summary>
    int PendingCount { get; }

    /// <summary>Hata uyarısı oluşturur; aynı özet bu oturumda zaten sorulduysa yalnızca teknik loga yazar.</summary>
    /// <param name="summary">Hata özeti (maskelenir).</param>
    /// <param name="component">Hatalı bileşen (kurulum/güncelleme).</param>
    /// <param name="step">Adım.</param>
    /// <returns>Gösterilecek rapor; tekrar ise <c>null</c>.</returns>
    AgentErrorReport? Raise(string summary, string? component = null, InstallStep? step = null);

    /// <summary>"Bildir": bağlı ve eşleşmişse gönderir; değilse (ya da gönderilemezse) bekletir.</summary>
    /// <param name="report">Rapor.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<ReportOutcome> ReportAsync(AgentErrorReport report, CancellationToken cancellationToken);

    /// <summary>"Hata raporunu dosyaya kaydet": gönderilecek metnin aynısını dosyaya yazar; API'ye bir şey gitmez.</summary>
    /// <param name="report">Rapor.</param>
    /// <param name="path">Kullanıcının seçtiği dosya.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task SaveToFileAsync(AgentErrorReport report, string path, CancellationToken cancellationToken);

    /// <summary>Bekleyen bildirimleri gönderir (bağlantı kurulunca kendiliğinden çağrılır).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task FlushAsync(CancellationToken cancellationToken);
}
