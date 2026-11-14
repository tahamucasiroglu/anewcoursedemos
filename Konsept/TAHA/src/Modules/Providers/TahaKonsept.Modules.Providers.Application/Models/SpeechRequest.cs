namespace TahaKonsept.Modules.Providers.Application.Models;

/// <summary>
/// Online ses modeline giden istek (somut ses modelleri ayrı bir adımda eklenecek). Metin Normalize aşamasından geçmiş okunuştur.
/// </summary>
public sealed record SpeechRequest
{
    /// <summary>Kullanıcı + amaç.</summary>
    public CallContext Context { get; init; } = new(Guid.Empty, "speech");

    /// <summary>Seslendirilecek metin (okunuş).</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>Ses kimliği (sağlayıcıya özgü).</summary>
    public string? Voice { get; init; }

    /// <summary>Dil (BCP-47, ör. <c>tr-TR</c>).</summary>
    public string Language { get; init; } = "tr-TR";

    /// <summary>Hız çarpanı (1 = normal).</summary>
    public decimal Speed { get; init; } = 1m;

    /// <summary>Çıktı biçimi (ör. <c>mp3</c>, <c>wav</c>).</summary>
    public string Format { get; init; } = "mp3";
}
