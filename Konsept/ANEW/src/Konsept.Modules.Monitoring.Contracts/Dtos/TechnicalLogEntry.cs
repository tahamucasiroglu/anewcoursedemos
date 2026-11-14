using Konsept.Modules.Monitoring.Contracts.Enums;

namespace Konsept.Modules.Monitoring.Contracts.Dtos;

/// <summary>Veritabanındaki teknik loga yazılacak kayıt (spec 0008 Req 6–7). Metinler Monitoring'de yeniden maskelenir.</summary>
/// <param name="OccurredAt">Log zamanı.</param>
/// <param name="Level">Seviye.</param>
/// <param name="Source">Kaynak uygulama.</param>
/// <param name="Message">Mesaj.</param>
/// <param name="Exception">Exception metni.</param>
/// <param name="CorrelationId">Korelasyon numarası.</param>
/// <param name="Category">Log kategorisi.</param>
/// <param name="AgentVersion">Ajan sürümü (yalnızca ajan özetinde).</param>
public sealed record TechnicalLogEntry(
    DateTimeOffset OccurredAt,
    LogEntryLevel Level,
    LogEntrySource Source,
    string Message,
    string? Exception = null,
    string? CorrelationId = null,
    string? Category = null,
    string? AgentVersion = null);
