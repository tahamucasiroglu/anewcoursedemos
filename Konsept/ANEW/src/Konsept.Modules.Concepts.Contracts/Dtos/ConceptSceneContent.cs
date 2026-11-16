namespace Konsept.Modules.Concepts.Contracts.Dtos;

/// <summary>Konseptin bir sahnesi, üretim hattının gördüğü biçimiyle (spec 0017 Req 7).</summary>
/// <param name="Title">Sahne başlığı.</param>
/// <param name="Visual">Ekran tarifi.</param>
/// <param name="Narration">Seslendirme metni.</param>
/// <param name="DurationSeconds">Süre (saniye).</param>
public sealed record ConceptSceneContent(string Title, string Visual, string Narration, int DurationSeconds);
