namespace TahaKonsept.Shared.Contracts.Diagnostics;

/// <summary>
/// Yönetim paneli istatistikleri: <c>GET api/diagnostics/admin/stats</c>. Değerler kayıtlı tüm <c>IStatsContributor</c>
/// katkıcılarının birleşiminden gelir; katkı vermeyen modülün alanları 0 / boş kalır. Seriler son 30 günü (Türkiye saati) boşluksuz kapsar.
/// </summary>
public sealed record AdminStatsResponse
{
    /// <summary>Kullanıcı sayısı.</summary>
    public int UserCount { get; init; }

    /// <summary>Toplam konsept.</summary>
    public int TotalConcepts { get; init; }

    /// <summary>Toplam üretilen video.</summary>
    public int TotalVideos { get; init; }

    /// <summary>Açık (çözülmemiş) hata bildirimi.</summary>
    public int OpenErrorReports { get; init; }

    /// <summary>Toplam günlük kaydı.</summary>
    public int TotalActivityLogs { get; init; }

    /// <summary>Bu ayki harcama ($).</summary>
    public decimal MonthSpendUsd { get; init; }

    /// <summary>Son 30 günde günlük üretilen video.</summary>
    public IReadOnlyList<DailyValue> VideosPerDay { get; init; } = [];

    /// <summary>Son 30 günde günlük harcama ($).</summary>
    public IReadOnlyList<DailyValue> SpendPerDayUsd { get; init; } = [];

    /// <summary>Son 30 günde seviyeye göre günlük kaydı sayıları.</summary>
    public IReadOnlyList<LevelCount> ActivityByLevel { get; init; } = [];

    /// <summary>Katkıcıların verdiği diğer sayısal / metin değerler (anahtar → değer).</summary>
    public IReadOnlyDictionary<string, object> Extra { get; init; } = new Dictionary<string, object>();

    /// <summary>Hata verdiği için atlanan katkıcılar (tür adları).</summary>
    public IReadOnlyList<string> FailedContributors { get; init; } = [];

    /// <summary>Hesaplanma anı (UTC).</summary>
    public DateTimeOffset GeneratedAt { get; init; }
}
