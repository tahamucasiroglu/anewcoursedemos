namespace Konsept.Modules.AgentGateway.Contracts.Dtos;

/// <summary>
/// Yerel aşamaya giden bir sahne (spec 0017 Req 7, 13): sıra, süre, seslendirme metni, görsel tarifi, video kodu ve (online görselde) Pi'de
/// üretilmiş görsel. API key ya da sağlayıcı bilgisi taşımaz (Y-7, Y-8).
/// </summary>
/// <param name="Index">Sahne sırası (0'dan).</param>
/// <param name="DurationSeconds">Süre (saniye).</param>
/// <param name="Narration">Seslendirme metni.</param>
/// <param name="ImagePrompt">Görsel tarifi ya da <c>null</c>.</param>
/// <param name="Code">Video kodu (render aşamasında) ya da <c>null</c>.</param>
/// <param name="ImageBase64">Online görsel (base64) ya da <c>null</c>.</param>
public sealed record AgentStageScene(int Index, int DurationSeconds, string Narration, string? ImagePrompt, string? Code, string? ImageBase64);
