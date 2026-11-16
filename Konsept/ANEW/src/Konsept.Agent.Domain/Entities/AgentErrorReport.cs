namespace Konsept.Agent.Domain.Entities;

/// <summary>
/// Ajandaki bir hata raporu (spec 0022 Req 4–8): uyarıda gösterilir, "Bildir" ile gönderilir ya da bekletilir, "Hata raporunu dosyaya
/// kaydet" ile dosyaya yazılır. Tüm metinler oluşturulurken maskelenmiştir; makine adı içermez.
/// </summary>
/// <param name="Id">Kimlik.</param>
/// <param name="CreatedAt">Oluşma zamanı (UTC).</param>
/// <param name="AccountId">Oluştuğu andaki hesap; eşleşmemişse <c>null</c> (ilk eşleşen hesapla gider).</param>
/// <param name="Summary">Maskelenmiş hata özeti.</param>
/// <param name="AgentVersion">Ajan versiyonu.</param>
/// <param name="OperatingSystem">İşletim sistemi.</param>
/// <param name="Component">Hatalı bileşen (kurulum/güncelleme).</param>
/// <param name="Step">Adım: indirme / doğrulama / kurulum.</param>
/// <param name="LogLines">Son 15 dakikanın Warning+ satırları (en fazla 200, eskiden yeniye).</param>
public sealed record AgentErrorReport(
    Guid Id,
    DateTimeOffset CreatedAt,
    Guid? AccountId,
    string Summary,
    string AgentVersion,
    string OperatingSystem,
    string? Component,
    string? Step,
    IReadOnlyList<string> LogLines);
