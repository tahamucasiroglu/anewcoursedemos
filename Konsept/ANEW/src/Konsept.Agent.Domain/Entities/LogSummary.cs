namespace Konsept.Agent.Domain.Entities;

/// <summary>Ajanın API'ye gönderdiği Error/Critical log özeti (spec 0008 Req 7). Mesaj maskelenmiştir.</summary>
/// <param name="OccurredAt">Log zamanı.</param>
/// <param name="Critical">Kritik mi (değilse Error).</param>
/// <param name="Message">Maskelenmiş mesaj.</param>
public sealed record LogSummary(DateTimeOffset OccurredAt, bool Critical, string Message);
