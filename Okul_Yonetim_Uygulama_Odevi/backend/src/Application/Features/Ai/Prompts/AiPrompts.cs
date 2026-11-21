using System.Reflection;

namespace OkulYonetim.Application.Features.Ai.Prompts;

/// <summary>
/// AI talimat metinleri (spec 0012): derlemeye gömülü <c>*.tr.txt</c> dosyalarından okunur — <see cref="VoiceMessage"/>
/// (sesli not dökümü → kibar veli mesajı) ve spec 0014 <see cref="WeeklySummary"/> (bir öğrencinin haftalık dayanağı → 3–5 cümlelik
/// gelişim özeti). Talimat sabittir; modele yalnızca talimat ve görevin verisi gider (veri minimizasyonu). Duman testi
/// (<c>tools/ai/smoke.mjs</c>) aynı dosyaları okur.
/// </summary>
public static class AiPrompts
{
    public const string VoiceMessageResource = "OkulYonetim.Ai.Prompts.voice-message.tr.txt";

    /// <summary>Sesli nottan veli mesajı talimatı; modele en fazla bu kadar token ürettirilir.</summary>
    public const int VoiceMessageMaxTokens = 400;

    public const string WeeklySummaryResource = "OkulYonetim.Ai.Prompts.weekly-summary.tr.txt";

    /// <summary>Haftalık özet talimatı; 800 karakterlik Türkçe metin için token sınırı.</summary>
    public const int WeeklySummaryMaxTokens = 400;

    private static readonly Lazy<string> VoiceMessageText = new(() => Read(VoiceMessageResource));

    private static readonly Lazy<string> WeeklySummaryText = new(() => Read(WeeklySummaryResource));

    public static string VoiceMessage => VoiceMessageText.Value;

    public static string WeeklySummary => WeeklySummaryText.Value;

    private static string Read(string name)
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
            ?? throw new InvalidOperationException("Gömülü AI talimatı bulunamadı: " + name);
        using StreamReader reader = new(stream);
        return reader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
    }
}
