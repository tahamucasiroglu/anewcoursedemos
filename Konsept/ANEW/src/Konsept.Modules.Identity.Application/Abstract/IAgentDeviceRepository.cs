using Konsept.Modules.Identity.Domain.Entities;

namespace Konsept.Modules.Identity.Application.Abstract;

/// <summary>Eşleşmiş ajan cihazlarının kalıcılık yüzeyi (spec 0012; uygulaması Infrastructure'da, iptal atomiktir).</summary>
public interface IAgentDeviceRepository
{
    /// <summary>Cihazı ekler.</summary>
    /// <param name="device">Cihaz.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task AddAsync(AgentDevice device, CancellationToken cancellationToken);

    /// <summary>Belirteç hash'iyle cihazı bulur.</summary>
    /// <param name="tokenHash">Belirtecin hash'i.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Cihaz ya da <c>null</c>.</returns>
    Task<AgentDevice?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>Son kullanım zamanını günceller (iptal edilmemiş cihazda).</summary>
    /// <param name="deviceId">Cihaz.</param>
    /// <param name="now">Kullanım zamanı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task TouchAsync(Guid deviceId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Kullanıcının iptal edilmemiş tüm cihazlarını tek işlemle iptal eder.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="now">İptal zamanı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İptal edilen cihazların kimlikleri.</returns>
    Task<IReadOnlyList<Guid>> RevokeActiveAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);
}
