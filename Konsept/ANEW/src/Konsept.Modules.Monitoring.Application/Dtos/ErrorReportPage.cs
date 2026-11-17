using Konsept.Modules.Monitoring.Domain.Entities;

namespace Konsept.Modules.Monitoring.Application.Dtos;

/// <summary>Hata bildirimlerinin bir sayfası ve filtreye uyan toplam (spec 0025).</summary>
/// <param name="Items">Bildirimler (en yeni üstte).</param>
/// <param name="Total">Toplam.</param>
public sealed record ErrorReportPage(IReadOnlyList<ErrorReport> Items, int Total);
