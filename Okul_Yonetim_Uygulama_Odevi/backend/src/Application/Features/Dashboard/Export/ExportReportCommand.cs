using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Dashboard.Reports;

namespace OkulYonetim.Application.Features.Dashboard.Export;

/// <summary>
/// Raporu Excel'de açılan CSV dosyası olarak verir (spec 0017 gereksinim 10–11): ekrandaki süzgeçle aynı satırların sayfasız tamamı ve
/// sonda toplam satır(lar)ı; en fazla 10.000 satır (aşan sonuç reddedilir); her başarılı dışa aktarma audit'e yazılır. Dosya sunucuda
/// saklanmaz ve loglanmaz; <c>ILoggableRequest</c> değildir (T9). Yalnızca kurucu.
/// </summary>
public sealed record ExportReportCommand(ReportKind Kind, DateOnly? StartDate, DateOnly? EndDate, Guid? BranchId, Guid? ClassroomId, int? MinAbsent, bool OverdueOnly)
    : IRequest<ReportFile>, IReportFilter;
