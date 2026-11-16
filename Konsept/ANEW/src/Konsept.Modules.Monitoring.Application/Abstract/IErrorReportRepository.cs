using Konsept.Modules.Monitoring.Domain.Entities;

namespace Konsept.Modules.Monitoring.Application.Abstract;

/// <summary>Hata bildirimlerinin kalıcılığı (spec 0022 Req 11): süresiz saklanır.</summary>
public interface IErrorReportRepository
{
    /// <summary>Bildirimi kaydeder; kaydedince <see cref="ErrorReport.Number"/> dolar.</summary>
    /// <param name="report">Bildirim.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task AddAsync(ErrorReport report, CancellationToken cancellationToken);

    /// <summary>Kullanıcının verilen andan sonraki web + ajan bildirimlerini sayar (API kayıtları sayılmaz, Req 15).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="since">Pencere başı (UTC).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sayı.</returns>
    Task<int> CountUserReportsSinceAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken);

    /// <summary>Numarayla bildirimi okur.</summary>
    /// <param name="number">Numara.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Bildirim ya da <c>null</c>.</returns>
    Task<ErrorReport?> GetByNumberAsync(long number, CancellationToken cancellationToken);
}
