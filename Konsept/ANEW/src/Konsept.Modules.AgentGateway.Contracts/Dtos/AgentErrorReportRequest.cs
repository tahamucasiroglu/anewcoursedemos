namespace Konsept.Modules.AgentGateway.Contracts.Dtos;

/// <summary>
/// Ajanın hub üzerinden gönderdiği hata bildirimi (spec 0022 Req 4, 8): hata özeti, ajan versiyonu, işletim sistemi, isteğe bağlı bileşen
/// ve adım, son 15 dakikanın Warning+ log satırları (en fazla 200). Makine adı alanı yoktur; metinler ajanda maskelenir, API'de yeniden
/// maskelenir. <see cref="AccountId"/> bildirimin oluştuğu andaki hesaptır (eşleşme öncesi <c>null</c>): sahiple uyuşmazsa kaydedilmez.
/// </summary>
/// <param name="AccountId">Bildirimin oluştuğu hesap.</param>
/// <param name="Summary">Hata özeti.</param>
/// <param name="AgentVersion">Ajan versiyonu.</param>
/// <param name="OperatingSystem">İşletim sistemi.</param>
/// <param name="Component">Hatalı bileşen.</param>
/// <param name="Step">Adım: indirme / doğrulama / kurulum.</param>
/// <param name="LogLines">Log satırları.</param>
public sealed record AgentErrorReportRequest(
    Guid? AccountId,
    string Summary,
    string? AgentVersion,
    string? OperatingSystem,
    string? Component,
    string? Step,
    IReadOnlyList<string>? LogLines);
