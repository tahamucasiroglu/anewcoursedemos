using Konsept.Modules.Monitoring.Contracts.Abstract;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Monitoring.Application.Abstract;

/// <summary>Teknik log servisi: yazma ve 30 günlük saklama (spec 0008 Req 6).</summary>
public interface ITechnicalLogService : ITechnicalLogWriter
{
    /// <summary>
    /// Web'in ilettiği logları yazar; gönderilen kaynak değeri ne olursa olsun kaynak Web olur. En fazla
    /// <see cref="MonitoringErrors.MaxLogsPerRequest"/> kayıt kabul edilir.
    /// </summary>
    /// <param name="entries">Kayıtlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Yazılan kayıt sayısı ya da hata.</returns>
    Task<Result<int>> AcceptWebLogsAsync(IReadOnlyList<TechnicalLogEntry> entries, CancellationToken cancellationToken);

    /// <summary>Saklama süresi dolan (30 günden eski) kayıtları siler.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Silinen kayıt sayısı.</returns>
    Task<int> PurgeExpiredAsync(CancellationToken cancellationToken);
}
