using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Media.Base;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Infrastructure.Media.Concrete;

/// <summary>
/// <see cref="VoiceCatalogBase"/> uygulaması: platform başına statik ses listeleri (aynı platformdaki modeller aynı listeyi paylaşır).
/// Azure'da Türkçeye özgü <c>tr-TR</c> sesleri ve çok dilli sesler; Gemini'de 30 hazır sesin seçkisi; OpenAI'nin yerleşik sesleri;
/// ElevenLabs'ın çok dilli hazır sesleri; Cartesia için doğrulanmamış örnek ses. Tüm modeller çok dilli olduğundan listedeki
/// her ses Türkçe okuyabilir; Türkçeye özgü olanlar <see cref="VoiceInfo.NativeTurkish"/> ile işaretlidir.
/// </summary>
public sealed class StaticVoiceCatalog : VoiceCatalogBase
{
    /// <summary>Kadın etiketi.</summary>
    private const string Female = "Kadın";

    /// <summary>Erkek etiketi.</summary>
    private const string Male = "Erkek";

    /// <summary>Nötr etiketi.</summary>
    private const string Neutral = "Nötr";

    /// <summary>Platform → sesler.</summary>
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<VoiceInfo>> Voices = new Dictionary<string, IReadOnlyList<VoiceInfo>>(StringComparer.Ordinal)
    {
        [ProviderPlatforms.AzureSpeech] =
        [
            Voice("tr-TR-AhmetNeural", "Ahmet (Türkçe)", "tr-TR", Male, nativeTurkish: true, isDefault: true),
            Voice("tr-TR-EmelNeural", "Emel (Türkçe)", "tr-TR", Female, nativeTurkish: true),
            Voice("en-US-AndrewMultilingualNeural", "Andrew (çok dilli)", "multi", Male, notes: "Türkçe <lang> etiketiyle okunur."),
            Voice("en-US-AvaMultilingualNeural", "Ava (çok dilli)", "multi", Female, notes: "Türkçe <lang> etiketiyle okunur."),
            Voice("en-US-BrianMultilingualNeural", "Brian (çok dilli)", "multi", Male, notes: "Türkçe <lang> etiketiyle okunur."),
            Voice("en-US-EmmaMultilingualNeural", "Emma (çok dilli)", "multi", Female, notes: "Türkçe <lang> etiketiyle okunur."),
        ],
        [ProviderPlatforms.Google] =
        [
            Voice("Kore", "Kore (kararlı)", "multi", Female, isDefault: true),
            Voice("Charon", "Charon (bilgilendirici)", "multi", Male),
            Voice("Puck", "Puck (neşeli)", "multi", Male),
            Voice("Zephyr", "Zephyr (parlak)", "multi", Female),
            Voice("Fenrir", "Fenrir (heyecanlı)", "multi", Male),
            Voice("Aoede", "Aoede (ferah)", "multi", Female),
            Voice("Orus", "Orus (kararlı)", "multi", Male),
            Voice("Leda", "Leda (genç)", "multi", Female),
            Voice("Iapetus", "Iapetus (net)", "multi", Male),
            Voice("Schedar", "Schedar (dengeli)", "multi", Male),
            Voice("Sulafat", "Sulafat (sıcak)", "multi", Female),
            Voice("Gacrux", "Gacrux (olgun)", "multi", Female),
        ],
        [ProviderPlatforms.OpenAi] =
        [
            Voice("coral", "Coral", "multi", Female, isDefault: true),
            Voice("onyx", "Onyx", "multi", Male),
            Voice("nova", "Nova", "multi", Female),
            Voice("ash", "Ash", "multi", Male),
            Voice("sage", "Sage", "multi", Female),
            Voice("echo", "Echo", "multi", Male),
            Voice("alloy", "Alloy", "multi", Neutral),
            Voice("ballad", "Ballad", "multi", Male),
            Voice("fable", "Fable", "multi", Neutral),
            Voice("shimmer", "Shimmer", "multi", Female),
            Voice("verse", "Verse", "multi", Male),
        ],
        [ProviderPlatforms.ElevenLabs] =
        [
            Voice("pNInz6obpgDQGcFmaJgB", "Adam", "multi", Male, isDefault: true, notes: "Hazır ses; derin anlatıcı."),
            Voice("onwK4e9ZLuTAKqWW03F9", "Daniel", "multi", Male, notes: "Hazır ses; haber spikeri tonu."),
            Voice("JBFqnCBsd6RMkjVDRZzb", "George", "multi", Male, notes: "Hazır ses; sıcak anlatıcı."),
            Voice("nPczCjzI2devNBz1zQrb", "Brian", "multi", Male, notes: "Hazır ses."),
            Voice("21m00Tcm4TlvDq8ikWAM", "Rachel", "multi", Female, notes: "Hazır ses; sakin."),
            Voice("EXAVITQu4vr4xnSDxMaL", "Sarah", "multi", Female, notes: "Hazır ses."),
            Voice("XrExE9yKIg1WjnnlVkGX", "Matilda", "multi", Female, notes: "Hazır ses."),
            Voice("pFZP5JQG7iQjIQuC4Bku", "Lily", "multi", Female, notes: "Hazır ses."),
        ],
        [ProviderPlatforms.Cartesia] =
        [
            Voice(
                "a0e99841-438c-4a64-b679-ae501e7d6091",
                "Örnek ses (Cartesia belgeleri)",
                "multi",
                Male,
                isDefault: true,
                notes: "Doğrulanmadı; Cartesia panelinden Türkçe bir ses kimliği seçip kullanın."),
        ],
    };

    /// <summary>Kurucu.</summary>
    /// <param name="catalog">Model kataloğu.</param>
    public StaticVoiceCatalog(IModelCatalog catalog)
        : base(catalog)
    {
    }

    /// <inheritdoc />
    protected override IReadOnlyList<VoiceInfo> VoicesFor(ModelInfo model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return Voices.TryGetValue(model.Platform, out var voices) ? voices : [];
    }

    /// <summary>Ses kaydı kurar.</summary>
    /// <param name="id">Ses kimliği.</param>
    /// <param name="name">Görünen ad.</param>
    /// <param name="language">Dil.</param>
    /// <param name="gender">Cinsiyet etiketi.</param>
    /// <param name="nativeTurkish">Türkçeye özgü mü.</param>
    /// <param name="isDefault">Varsayılan mı.</param>
    /// <param name="notes">Not.</param>
    private static VoiceInfo Voice(string id, string name, string language, string gender, bool nativeTurkish = false, bool isDefault = false, string? notes = null) =>
        new()
        {
            Id = id,
            DisplayName = name,
            Language = language,
            Gender = gender,
            NativeTurkish = nativeTurkish,
            IsDefault = isDefault,
            Notes = notes,
        };
}
