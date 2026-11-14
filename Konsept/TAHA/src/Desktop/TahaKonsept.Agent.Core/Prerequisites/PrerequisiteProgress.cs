namespace TahaKonsept.Agent.Core.Prerequisites;

/// <summary>
/// Kurulum ekranına gösterilecek ilerleme: adım adı/sırası, adım yüzdesi, genel yüzde ve (varsa) günlük satırı.
/// </summary>
/// <param name="StepId">Adım (bileşen) kimliği.</param>
/// <param name="StepName">Adım adı (ör. <c>Node.js</c>).</param>
/// <param name="StepIndex">Adım sırası (0 tabanlı).</param>
/// <param name="StepCount">Toplam adım.</param>
/// <param name="StepPercent">Adım yüzdesi (bilinmiyorsa <c>null</c>).</param>
/// <param name="OverallPercent">Genel yüzde (0–100).</param>
/// <param name="Message">Kısa durum mesajı.</param>
/// <param name="LogLine">Günlük satırı (ör. süreç çıktısı; yoksa <c>null</c>).</param>
public sealed record PrerequisiteProgress(
    string StepId,
    string StepName,
    int StepIndex,
    int StepCount,
    double? StepPercent,
    double OverallPercent,
    string Message,
    string? LogLine = null);
