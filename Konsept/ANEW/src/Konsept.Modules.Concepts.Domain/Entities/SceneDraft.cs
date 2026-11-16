namespace Konsept.Modules.Concepts.Domain.Entities;

/// <summary>Elle düzenlenmiş, henüz doğrulanmamış sahne (spec 0015 Req 3–4).</summary>
/// <param name="Title">Sahne başlığı.</param>
/// <param name="Visual">Ekran tarifi.</param>
/// <param name="Narration">Seslendirme metni.</param>
/// <param name="DurationSeconds">Süre; tam sayı değilse <c>null</c>.</param>
public sealed record SceneDraft(string? Title, string? Visual, string? Narration, int? DurationSeconds);
