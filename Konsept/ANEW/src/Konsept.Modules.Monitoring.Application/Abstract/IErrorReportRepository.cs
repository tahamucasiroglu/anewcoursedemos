using Konsept.Modules.Monitoring.Application.Dtos;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;

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

    /// <summary>Bildirimlerin bir sayfası (spec 0025): en yeni (büyük numara) üstte; kaynak ve durum kümesine göre süzülür.</summary>
    /// <param name="source">Kaynak; <c>null</c> tümü.</param>
    /// <param name="statuses">Durum kümesi; <c>null</c> tümü.</param>
    /// <param name="skip">Atlanacak.</param>
    /// <param name="take">Alınacak.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sayfa.</returns>
    Task<ErrorReportPage> ListAsync(ErrorReportSource? source, IReadOnlyCollection<ErrorReportStatus>? statuses, int skip, int take, CancellationToken cancellationToken);

    /// <summary>Bildirimin durumunu ve admin notunu tek ifadede değiştirir (spec 0025 Req 5).</summary>
    /// <param name="number">Numara.</param>
    /// <param name="status">Yeni durum.</param>
    /// <param name="note">Admin notu (maskeli, kırpılmış); <c>null</c> siler.</param>
    /// <param name="at">Değişiklik anı (UTC).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Bildirim varsa ve değiştiyse <c>true</c>.</returns>
    Task<bool> UpdateStatusAsync(long number, ErrorReportStatus status, string? note, DateTimeOffset at, CancellationToken cancellationToken);
}
