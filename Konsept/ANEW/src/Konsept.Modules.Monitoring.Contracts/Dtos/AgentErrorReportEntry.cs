namespace Konsept.Modules.Monitoring.Contracts.Dtos;

/// <summary>
/// AgentGateway'in Monitoring'e ilettiği ajan bildirimi (spec 0022 Req 4): hata özeti, ajan versiyonu, işletim sistemi, isteğe bağlı
/// bileşen ve adım, log satırları. Makine adı alanı yoktur; her metin Monitoring'de yeniden maskelenir.
/// </summary>
/// <param name="Summary">Hata özeti.</param>
/// <param name="AgentVersion">Ajan versiyonu.</param>
/// <param name="OperatingSystem">İşletim sistemi.</param>
/// <param name="Component">Hatalı bileşen (kurulum/güncelleme).</param>
/// <param name="Step">Adım: indirme / doğrulama / kurulum.</param>
/// <param name="LogLines">Son 15 dakikanın Warning+ log satırları (en fazla 200).</param>
public sealed record AgentErrorReportEntry(
    string? Summary,
    string? AgentVersion,
    string? OperatingSystem,
    string? Component,
    string? Step,
    IReadOnlyList<string>? LogLines);
