using System.Globalization;

namespace TahaKonsept.Modules.Production.Normalization.Text;

/// <summary>
/// Türkçe kültür yardımcıları: <c>tr-TR</c> kültürüyle büyük/küçük harf (i/İ, ı/I), baş harf büyütme, eşleşme için katlama
/// (<see cref="FoldForMatch"/>: ı ile i aynı sayılır), ünlü/ünsüz sınıfları (kalın/ince, sert ünsüz) ve ünlü uyumu.
/// Tüm dönüşümler uzunluğu korur (indeks eşlemesi için önemli).
/// </summary>
public static class TurkishCulture
{
    /// <summary><c>tr-TR</c> kültürü (ICU yoksa sabit kültüre düşer; i/I eşlemesi yine elle yapılır).</summary>
    public static CultureInfo Culture { get; } = CreateCulture();

    /// <summary>Türkçe küçük harf (I → ı, İ → i).</summary>
    /// <param name="value">Metin.</param>
    /// <returns>Küçük harfli metin (aynı uzunlukta).</returns>
    public static string ToLower(string value) => value.Replace('I', 'ı').Replace('İ', 'i').ToLower(Culture);

    /// <summary>Türkçe büyük harf (i → İ, ı → I).</summary>
    /// <param name="value">Metin.</param>
    /// <returns>Büyük harfli metin.</returns>
    public static string ToUpper(string value) => value.Replace('i', 'İ').Replace('ı', 'I').ToUpper(Culture);

    /// <summary>İlk harfi Türkçe kurala göre büyütür ("ikinci" → "İkinci").</summary>
    /// <param name="value">Metin.</param>
    /// <returns>Baş harfi büyük metin.</returns>
    public static string Capitalize(string value) =>
        value.Length == 0 ? value : ToUpper(value[..1]) + value[1..];

    /// <summary>Baş harf büyük, kalanı küçük ("NATO" → "Nato").</summary>
    /// <param name="value">Metin.</param>
    /// <returns>Dönüştürülmüş metin.</returns>
    public static string ToTitle(string value) => value.Length == 0 ? value : Capitalize(ToLower(value));

    /// <summary>Eşleştirme anahtarı: Türkçe küçük harf + ı → i (İngilizce "Instagram" ile "İnstagram" aynı anahtara düşer).</summary>
    /// <param name="value">Metin.</param>
    /// <returns>Aynı uzunlukta katlanmış metin.</returns>
    public static string FoldForMatch(string value) => ToLower(value).Replace('ı', 'i');

    /// <summary>Türkçe ünlü mü (a, e, ı, i, o, ö, u, ü, â, î, û; küçük harf).</summary>
    /// <param name="c">Karakter.</param>
    /// <returns>Ünlüyse <c>true</c>.</returns>
    public static bool IsVowel(char c) => c is 'a' or 'e' or 'ı' or 'i' or 'o' or 'ö' or 'u' or 'ü' or 'â' or 'î' or 'û';

    /// <summary>Kalın ünlü mü (a, ı, o, u).</summary>
    /// <param name="c">Küçük harf ünlü.</param>
    /// <returns>Kalınsa <c>true</c>.</returns>
    public static bool IsBackVowel(char c) => c is 'a' or 'ı' or 'o' or 'u' or 'â' or 'û';

    /// <summary>Sert (ötümsüz) ünsüz mü: f, s, t, k, ç, ş, h, p ("fıstıkçı şahap").</summary>
    /// <param name="c">Küçük harf.</param>
    /// <returns>Sertse <c>true</c>.</returns>
    public static bool IsVoiceless(char c) => c is 'f' or 's' or 't' or 'k' or 'ç' or 'ş' or 'h' or 'p';

    /// <summary>Metnin (küçük harfe çevrilmiş) son ünlüsü; yoksa <c>null</c>.</summary>
    /// <param name="value">Metin.</param>
    /// <returns>Son ünlü.</returns>
    public static char? LastVowel(string value)
    {
        var lower = ToLower(value);
        for (var i = lower.Length - 1; i >= 0; i--)
        {
            if (IsVowel(lower[i]))
            {
                return Plain(lower[i]);
            }
        }

        return null;
    }

    /// <summary>Metnin (küçük harfe çevrilmiş) son harfi; harf yoksa <c>null</c>.</summary>
    /// <param name="value">Metin.</param>
    /// <returns>Son harf.</returns>
    public static char? LastLetter(string value)
    {
        var lower = ToLower(value);
        for (var i = lower.Length - 1; i >= 0; i--)
        {
            if (char.IsLetter(lower[i]))
            {
                return Plain(lower[i]);
            }
        }

        return null;
    }

    /// <summary>İki yönlü ünlü uyumu: kalınsa <c>a</c>, inceyse <c>e</c>.</summary>
    /// <param name="previousVowel">Önceki ünlü.</param>
    /// <returns>a ya da e.</returns>
    public static char TwoWay(char previousVowel) => IsBackVowel(previousVowel) ? 'a' : 'e';

    /// <summary>Dört yönlü ünlü uyumu: a/ı → ı, e/i → i, o/u → u, ö/ü → ü.</summary>
    /// <param name="previousVowel">Önceki ünlü.</param>
    /// <returns>ı, i, u ya da ü.</returns>
    public static char FourWay(char previousVowel) => previousVowel switch
    {
        'a' or 'ı' => 'ı',
        'o' or 'u' => 'u',
        'ö' or 'ü' => 'ü',
        _ => 'i',
    };

    private static char Plain(char c) => c switch
    {
        'â' => 'a',
        'î' => 'i',
        'û' => 'u',
        _ => c,
    };

    private static CultureInfo CreateCulture()
    {
        try
        {
            return CultureInfo.GetCultureInfo("tr-TR");
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture;
        }
    }
}
