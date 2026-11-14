namespace Konsept.Web.ApiClient.Dtos;

/// <summary>
/// Günlük şeridinde gösterilen olay.
/// </summary>
/// <param name="Type">Olay türü.</param>
/// <param name="Level">Seviye: 0 bilgi, 1 uyarı, 2 hata.</param>
/// <param name="Message">Türkçe metin.</param>
/// <param name="OccurredAt">Olay zamanı (UTC).</param>
public sealed record SystemEventResponse(string Type, int Level, string Message, DateTimeOffset OccurredAt);
