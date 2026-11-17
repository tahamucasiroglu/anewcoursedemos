namespace Konsept.Modules.Monitoring.Contracts.Dtos;

/// <summary>Admin İstatistikler bölümü (spec 0010 Req 3–4): özet sayılar ve seçilen aralığın günlük serisi.</summary>
/// <param name="TotalUsers">Toplam kullanıcı.</param>
/// <param name="TotalClips">Üretilen toplam klip.</param>
/// <param name="OpenErrorReports">Açık hata bildirimi.</param>
/// <param name="Days">Aralık (7 / 30 / 90).</param>
/// <param name="Series">Günler (eskiden yeniye; veri olmayan gün 0).</param>
/// <param name="BannedUsers">Banlı kullanıcı sayısı (spec 0024 Req 9).</param>
public sealed record AdminStatisticsDto(int TotalUsers, int TotalClips, int OpenErrorReports, int Days, IReadOnlyList<AdminDayDto> Series, int BannedUsers = 0);
