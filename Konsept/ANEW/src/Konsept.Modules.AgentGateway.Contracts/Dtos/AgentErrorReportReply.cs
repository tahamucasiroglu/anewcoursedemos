namespace Konsept.Modules.AgentGateway.Contracts.Dtos;

/// <summary>Ajan hata bildiriminin sonucu (spec 0022 Req 4, 8, 15).</summary>
/// <param name="Result">Sonuç.</param>
/// <param name="Number">Kaydedildiyse bildirim numarası.</param>
public sealed record AgentErrorReportReply(AgentErrorReportResult Result, long? Number = null);
