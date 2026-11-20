using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Ai.VoiceMessages;

/// <summary>
/// Sesli nottan veli mesajı taslağı (spec 0012 gereksinim 1): konuşma, ses (bellekte; en fazla <see cref="MaxAudioBytes"/>)
/// ve içerik türü. Veliye hiçbir şey göndermez — yalnızca <c>AiDraft</c> üretir (BR-5). Ses ve metin taşıdığı için loglanmaz (T9).
/// </summary>
public sealed record CreateVoiceMessageDraftCommand(Guid? ConversationId, ReadOnlyMemory<byte> Audio, string? ContentType) : IRequest<VoiceMessageDraftDto>
{
    public const int MaxAudioBytes = 2 * 1024 * 1024;

    public const double MaxDurationSeconds = 60.0;

    /// <summary>Telefon kaydı biçimleri (m4a/aac, webm, wav, ogg); parametreler (<c>;codecs=…</c>) yok sayılır.</summary>
    public static readonly IReadOnlySet<string> SupportedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "audio/m4a", "audio/x-m4a", "audio/mp4", "audio/aac", "audio/webm", "audio/wav", "audio/x-wav", "audio/wave", "audio/ogg",
    };

    public static bool IsSupported(string? contentType) =>
        !string.IsNullOrWhiteSpace(contentType) && SupportedTypes.Contains(contentType.Split(';')[0].Trim());
}
