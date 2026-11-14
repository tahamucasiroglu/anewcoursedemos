namespace Konsept.Modules.AgentGateway.Contracts.Dtos;

/// <summary>
/// Ajanın API'ye bağlıyken gönderdiği Error/Critical log özeti (spec 0008 Req 7). Tam log ajanda kalır; mesaj
/// ajanda maskelenir, API'de yeniden maskelenir.
/// </summary>
/// <param name="Level">Seviye: 4 hata, 5 kritik (<c>Microsoft.Extensions.Logging.LogLevel</c> değerleri).</param>
/// <param name="OccurredAt">Log zamanı.</param>
/// <param name="Message">Maskelenmiş mesaj.</param>
/// <param name="AgentVersion">Ajan sürümü.</param>
public sealed record AgentLogSummary(int Level, DateTimeOffset OccurredAt, string Message, string AgentVersion);
