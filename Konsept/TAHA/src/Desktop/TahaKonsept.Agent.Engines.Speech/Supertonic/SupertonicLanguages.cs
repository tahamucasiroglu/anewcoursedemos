namespace TahaKonsept.Agent.Engines.Speech.Supertonic;

/// <summary>Supertonic 3'ün desteklediği dil kodları (31 dil + <c>na</c>: dil etiketsiz). Metin <c>&lt;tr&gt;…&lt;/tr&gt;</c> ile sarılır.</summary>
internal static class SupertonicLanguages
{
    /// <summary>Resmi örnek koddaki dil listesi.</summary>
    public static readonly IReadOnlyList<string> All =
    [
        "en", "ko", "ja", "ar", "bg", "cs", "da", "de", "el", "es", "et", "fi", "fr", "hi", "hr", "hu",
        "id", "it", "lt", "lv", "nl", "pl", "pt", "ro", "ru", "sk", "sl", "sv", "tr", "uk", "vi", "na",
    ];
}
