namespace Konsept.Modules.Concepts.Application.Dtos;

/// <summary>Üretilenler satırı: başlık, durum kodu, uyarılar ve zaman.</summary>
/// <param name="Id">Kimlik.</param>
/// <param name="Title">Başlık.</param>
/// <param name="Status">Durum kodu.</param>
/// <param name="WordRepetitionWarning">Kelime tekrarı uyarısı.</param>
/// <param name="DurationMismatchWarning">Süre uyumsuz uyarısı.</param>
/// <param name="CreatedAt">Oluşturulma (UTC).</param>
public sealed record ConceptListItemDto(Guid Id, string Title, string Status, bool WordRepetitionWarning, bool DurationMismatchWarning, DateTimeOffset CreatedAt);
