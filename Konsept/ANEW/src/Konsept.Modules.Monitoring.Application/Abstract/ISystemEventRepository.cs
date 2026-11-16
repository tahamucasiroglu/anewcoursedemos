using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;

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

    /// <summary>Kullanıcının kendi en son olayını getirir (spec 0021 Req 1, 7: Günlük yalnızca kendi kayıtları).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Olay ya da <c>null</c>.</returns>
    Task<SystemEvent?> GetLatestOwnedAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Kullanıcının bir zaman aralığındaki olaylarını en yenisi üstte listeler (Günlük geçmişi, spec 0021 Req 5).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="fromUtc">Aralık başı (dahil).</param>
    /// <param name="toUtc">Aralık sonu (hariç).</param>
    /// <param name="levels">Seviyeler; <c>null</c> ise hepsi.</param>
    /// <param name="skip">Atlanacak kayıt sayısı.</param>
    /// <param name="take">Alınacak en fazla kayıt sayısı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Olaylar.</returns>
    Task<IReadOnlyList<SystemEvent>> ListOwnedAsync(
        Guid userId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        IReadOnlyList<EventLevel>? levels,
        int skip,
        int take,
        CancellationToken cancellationToken);

    /// <summary>Kullanıcının en eski olayının zamanını getirir.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Zaman ya da <c>null</c>.</returns>
    Task<DateTimeOffset?> GetEarliestOwnedAsync(Guid userId, CancellationToken cancellationToken);
}
