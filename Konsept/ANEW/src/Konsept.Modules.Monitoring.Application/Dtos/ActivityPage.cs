using Konsept.Modules.Monitoring.Domain.Entities;

namespace Konsept.Modules.Monitoring.Application.Dtos;

/// <summary>Kullanıcılı olayların bir sayfası ve filtreye uyan toplam.</summary>
/// <param name="Items">Olaylar (en yeni üstte).</param>
/// <param name="Total">Toplam.</param>
public sealed record ActivityPage(IReadOnlyList<SystemEvent> Items, int Total);
