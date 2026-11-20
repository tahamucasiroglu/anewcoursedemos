namespace OkulYonetim.Application.Features.Ai.VoiceMessages;

/// <summary>Sesli not taslağı yanıtı (spec 0012 AC-1): taslak kimliği (göndermede verilir), kaynak döküm (BR-6), taslak metin (en fazla 1000) ve ses süresi.</summary>
public sealed record VoiceMessageDraftDto(Guid DraftId, string Transcript, string Draft, double DurationSeconds);
