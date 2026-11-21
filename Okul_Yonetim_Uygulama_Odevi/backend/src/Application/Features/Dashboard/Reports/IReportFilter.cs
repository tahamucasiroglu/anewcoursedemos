namespace OkulYonetim.Application.Features.Dashboard.Reports;

/// <summary>
/// Rapor süzgeci (spec 0017 gereksinim 6–9): tarih aralığı (devamsızlık ve tahsilat; borç raporunda yok), şube, sınıf ve "en az N gün yok"
/// eşiği (devamsızlık), "yalnızca gecikenler" (borç). Ekran sorgusu ve dışa aktarma aynı süzgeci taşır — dosya ekrandaki satırların
/// sayfasız tamamıdır.
/// </summary>
public interface IReportFilter
{
    DateOnly? StartDate { get; }

    DateOnly? EndDate { get; }

    Guid? BranchId { get; }

    Guid? ClassroomId { get; }

    int? MinAbsent { get; }

    bool OverdueOnly { get; }
}
