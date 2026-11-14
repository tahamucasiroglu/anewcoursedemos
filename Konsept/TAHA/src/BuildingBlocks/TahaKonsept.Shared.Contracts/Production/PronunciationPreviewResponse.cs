namespace TahaKonsept.Shared.Contracts.Production;

/// <summary>
/// Okunuş önizleme sonucu: normalize edilmiş metin ve çalınabilir ses (yerel: ajanın sunucusundaki adres; online: base64 <c>data:</c> adresi).
/// </summary>
public sealed record PronunciationPreviewResponse
{
    /// <summary>Yazılış.</summary>
    public string DisplayText { get; init; } = string.Empty;

    /// <summary>Okunuş (TTS'e giden).</summary>
    public string SpokenText { get; init; } = string.Empty;

    /// <summary>Ses adresi (ajan yerel sunucusu ya da <c>data:</c> adresi).</summary>
    public string? AudioUrl { get; init; }

    /// <summary>Ses içeriği (online önizleme).</summary>
    public string? AudioBase64 { get; init; }

    /// <summary>Ses türü.</summary>
    public string? MimeType { get; init; }

    /// <summary>Kaynak: <c>agent</c>, <c>online</c> ya da <c>none</c> (ses kapalı / üretilemedi).</summary>
    public string Source { get; init; } = "none";

    /// <summary>Bilgi/hata mesajı.</summary>
    public string? Message { get; init; }
}
