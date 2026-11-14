namespace TahaKonsept.Modules.Production.Normalization.Text;

/// <summary>
/// Sözcük gibi okunan kısaltmalar ve büyük harfle yazılmış kısa Türkçe sözcükler (harf harf OKUNMAZ):
/// NATO → Nato, NASA → Nasa, UNESCO → Unesko, TÜBİTAK → Tübitak, YÖK → Yök, VE → ve … Önemli üye: <see cref="Words"/>.
/// </summary>
public static class KnownAcronyms
{
    /// <summary>Büyük harfli yazılış → okunuş.</summary>
    public static IReadOnlyDictionary<string, string> Words { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["NATO"] = "Nato", ["NASA"] = "Nasa", ["UNESCO"] = "Unesko", ["UNICEF"] = "Yunisef", ["FIFA"] = "Fifa",
        ["UEFA"] = "Uefa", ["OPEC"] = "Opek", ["ASELSAN"] = "Aselsan", ["ROKETSAN"] = "Roketsan", ["HAVELSAN"] = "Havelsan",
        ["TUSAŞ"] = "Tusaş", ["BOTAŞ"] = "Botaş", ["TÜBİTAK"] = "Tübitak", ["TÜSİAD"] = "Tüsiad", ["TÜİK"] = "Tüik",
        ["TOKİ"] = "Toki", ["KOSGEB"] = "Kosgeb", ["ODTÜ"] = "Odtü", ["YÖK"] = "Yök", ["MEB"] = "Meb", ["MİT"] = "Mit",
        ["DEAŞ"] = "Deaş", ["İSKİ"] = "İski", ["ASKİ"] = "Aski", ["TEKEL"] = "Tekel", ["RAM"] = "Ram", ["ROM"] = "Rom",
        ["PIN"] = "Pin", ["LASER"] = "Lazer", ["RADAR"] = "Radar", ["COVID"] = "Kovid", ["JPEG"] = "Jeypeg",
        ["TL"] = "Türk lirası", ["VE"] = "ve", ["BU"] = "bu", ["ŞU"] = "şu", ["BİR"] = "bir", ["ÇOK"] = "çok", ["DE"] = "de",
        ["DA"] = "da", ["Kİ"] = "ki", ["İLE"] = "ile", ["NE"] = "ne", ["EN"] = "en", ["YA"] = "ya", ["AMA"] = "ama",
        ["OL"] = "ol", ["ON"] = "on", ["AY"] = "ay", ["YIL"] = "yıl", ["SU"] = "su", ["EV"] = "ev", ["YOK"] = "yok",
        ["VAR"] = "var", ["SON"] = "son", ["YENİ"] = "yeni",
    };
}
