namespace Konsept.Modules.Production.Domain.Entities;

/// <summary>Analiz aşamasının bir sahne için planı (spec 0017 Req 7): sıra, süre, seslendirme bölümü ve görsel tarifi.</summary>
/// <param name="Index">Sahne sırası (0'dan).</param>
/// <param name="DurationSeconds">Süre (saniye, pozitif).</param>
/// <param name="Narration">Seslendirme metni.</param>
/// <param name="ImagePrompt">Görsel tarifi.</param>
public sealed record ScenePlan(int Index, int DurationSeconds, string Narration, string ImagePrompt);
