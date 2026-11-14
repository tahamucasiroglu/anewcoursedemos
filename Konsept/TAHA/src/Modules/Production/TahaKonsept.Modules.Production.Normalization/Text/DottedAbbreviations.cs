namespace TahaKonsept.Modules.Production.Normalization.Text;

/// <summary>
/// Noktalı kısaltmalar: <see cref="Titles"/> (isimden önce gelen unvanlar: Dr., Prof. …; cümle sonu sayılmaz) ve
/// <see cref="Trailing"/> (yy., vb., vs. …; cümle sonundaysa okunuşa nokta korunur). Büyük/küçük harf duyarlı.
/// </summary>
public static class DottedAbbreviations
{
    /// <summary>Unvanlar.</summary>
    public static IReadOnlyDictionary<string, string> Titles { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Prof. Dr."] = "profesör doktor", ["Doç. Dr."] = "doçent doktor", ["Dr."] = "doktor", ["Prof."] = "profesör",
        ["Doç."] = "doçent", ["Av."] = "avukat", ["Sn."] = "sayın", ["Hz."] = "hazreti", ["Yrd."] = "yardımcı",
        ["Op."] = "operatör", ["Uzm."] = "uzman", ["Öğr."] = "öğretmen", ["Gör."] = "görevlisi", ["Org."] = "orgeneral",
        ["Alb."] = "albay", ["Bkz."] = "bakınız", ["bkz."] = "bakınız", ["örn."] = "örneğin", ["Örn."] = "örneğin",
    };

    /// <summary>Sonda gelebilen kısaltmalar.</summary>
    public static IReadOnlyDictionary<string, string> Trailing { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["yy."] = "yüzyıl", ["vb."] = "ve benzeri", ["vs."] = "vesaire", ["vd."] = "ve diğerleri", ["Ltd."] = "limited",
        ["Şti."] = "şirketi", ["A.Ş."] = "anonim şirketi", ["Cad."] = "caddesi", ["Sok."] = "sokağı", ["Mah."] = "mahallesi",
        ["dk."] = "dakika", ["sn."] = "saniye", ["T.C."] = "te ce",
    };
}
