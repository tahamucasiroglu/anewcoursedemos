namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>Sürümün üretildiği konsept metnindeki sahne (spec 0007 Req 5): LLM çıktısı, düz metin olarak gösterilir.</summary>
/// <param name="Title">Kısa başlık.</param>
/// <param name="Visual">Görsel tarifi.</param>
/// <param name="Narration">Seslendirme metni.</param>
/// <param name="DurationSeconds">Süre (sn).</param>
public sealed record ProducedSceneDto(string Title, string Visual, string Narration, int DurationSeconds);
