using System.Reflection;

namespace OkulYonetim.Application.Features.Ai.Prompts;

/// <summary>
/// AI talimat metinleri (spec 0012): derlemeye gömülü <c>*.tr.txt</c> dosyalarından okunur — <see cref="VoiceMessage"/>
/// (sesli not dökümü → kibar veli mesajı). Talimat sabittir; modele yalnızca talimat ve döküm gider (gereksinim 6).
/// Duman testi (<c>tools/ai/smoke.mjs</c>) aynı dosyayı okur.
/// </summary>
public static class AiPrompts
{
    public const string VoiceMessageResource = "OkulYonetim.Ai.Prompts.voice-message.tr.txt";

    /// <summary>Sesli nottan veli mesajı talimatı; modele en fazla bu kadar token ürettirilir.</summary>
    public const int VoiceMessageMaxTokens = 400;

    private static readonly Lazy<string> VoiceMessageText = new(() => Read(VoiceMessageResource));

    public static string VoiceMessage => VoiceMessageText.Value;

    private static string Read(string name)
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
            ?? throw new InvalidOperationException("Gömülü AI talimatı bulunamadı: " + name);
        using StreamReader reader = new(stream);
        return reader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
    }
}
