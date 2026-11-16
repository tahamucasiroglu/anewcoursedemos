using Konsept.Modules.Production.Domain.Entities;

namespace Konsept.Modules.Production.Application.Abstract;

/// <summary>Kullanıcının sıra durumu (art arda hata, durdu uyarısı; spec 0017 Req 11).</summary>
public interface IQueueStateRepository
{
    /// <summary>Durumu okur.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum ya da <c>null</c> (hiç hata yok).</returns>
    Task<QueueState?> GetAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Durumu yazar.</summary>
    /// <param name="state">Durum.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task SetAsync(QueueState state, CancellationToken cancellationToken);
}
