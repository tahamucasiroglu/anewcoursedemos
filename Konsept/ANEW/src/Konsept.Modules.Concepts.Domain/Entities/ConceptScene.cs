namespace Konsept.Modules.Concepts.Domain.Entities;

/// <summary>Konseptin bir sahnesi (spec 0014 Req 10): başlık, ekran tarifi, seslendirme metni, süre (pozitif tam saniye).</summary>
/// <param name="Title">Sahne başlığı.</param>
/// <param name="Visual">Ekranda gösterilecek şeyin tarifi.</param>
/// <param name="Narration">Seslendirme metni.</param>
/// <param name="DurationSeconds">Süre (saniye).</param>
public sealed record ConceptScene(string Title, string Visual, string Narration, int DurationSeconds);
