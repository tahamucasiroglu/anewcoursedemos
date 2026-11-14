using Konsept.Modules.Monitoring.Domain.Entities;

namespace Konsept.Modules.Monitoring.Application.Abstract;

/// <summary>
/// Olay kaydının kalıcılık yüzeyi (uygulaması Infrastructure'da).
/// </summary>
public interface ISystemEventRepository
{
    /// <summary>Olayı kalıcı olarak ekler.</summary>
    /// <param name="systemEvent">Olay.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task AddAsync(SystemEvent systemEvent, CancellationToken cancellationToken);

    /// <summary>En son olayı getirir.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>En son olay ya da <c>null</c>.</returns>
    Task<SystemEvent?> GetLatestAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Kullanıcının Günlük şeridinde görebileceği en son olayı getirir (spec 0002 Req 18, plan Risk 7): kullanıcının kendi
    /// olayları ve kullanıcısız sistem/ajan olayları; başka kullanıcının olayı asla.
    /// </summary>
    /// <param name="userId">Kullanıcı; giriş yapılmamışsa <c>null</c> (yalnızca kullanıcısız olaylar).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Olay ya da <c>null</c>.</returns>
    Task<SystemEvent?> GetLatestVisibleAsync(Guid? userId, CancellationToken cancellationToken);
}
