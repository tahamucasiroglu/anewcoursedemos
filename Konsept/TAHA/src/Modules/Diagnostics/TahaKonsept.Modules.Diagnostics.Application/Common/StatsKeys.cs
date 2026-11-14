namespace TahaKonsept.Modules.Diagnostics.Application.Common;

/// <summary>
/// <see cref="Abstract.IStatsContributor"/> sözlüklerinde tanınan anahtarlar ve beklenen değer biçimleri.
/// Sayılar: <c>int</c>/<c>long</c>/<c>decimal</c>/<c>double</c> (hepsi kabul). Günlük seriler (Türkiye saatine göre gün):
/// <c>IReadOnlyDictionary&lt;DateOnly, int|decimal&gt;</c> (DateTime / "yyyy-MM-dd" anahtar da olur) ya da <c>IEnumerable&lt;DailyValue&gt;</c>.
/// Aynı anahtarı iki katkıcı verirse sonra kaydedilen kazanır. Burada olmayan sayısal/metin/bool anahtarlar yanıtın <c>Extra</c> alanına gider.
/// </summary>
public static class StatsKeys
{
    /// <summary>Kullanıcı sayısı (Identity).</summary>
    public const string UsersTotal = "users.total";

    /// <summary>Toplam konsept (Concepts).</summary>
    public const string ConceptsTotal = "concepts.total";

    /// <summary>Toplam üretilen video (Production).</summary>
    public const string VideosTotal = "videos.total";

    /// <summary>Günlük üretilen video serisi (Production).</summary>
    public const string VideosPerDay = "videos.perDay";

    /// <summary>Günlük harcama serisi, dolar (Providers).</summary>
    public const string SpendPerDayUsd = "spend.perDayUsd";

    /// <summary>Bu ayki harcama, dolar (Providers).</summary>
    public const string SpendMonthUsd = "spend.monthUsd";

    /// <summary>Açık hata bildirimi (Diagnostics).</summary>
    public const string ErrorReportsOpen = "errorReports.open";

    /// <summary>Toplam günlük kaydı (Diagnostics).</summary>
    public const string ActivityTotal = "activity.total";

    /// <summary>Son 30 günde seviyeye göre günlük kaydı: <c>IReadOnlyDictionary&lt;LogLevelKind, int&gt;</c> (Diagnostics).</summary>
    public const string ActivityByLevel = "activity.byLevel";
}
