using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Agents.Domain.Entities;

namespace TahaKonsept.Modules.Agents.Application.Abstract;

/// <summary>
/// Eşlenmiş cihaz deposu. Önemli metotlar: <see cref="ListActiveAsync"/> (iptal edilmemiş cihazlar),
/// <see cref="FindActiveByMachineAsync"/> (aynı bilgisayar yeniden eşlenince kaydı yeniden kullanmak için).
/// </summary>
public interface IAgentDeviceRepository : IRepository<AgentDevice>
{
    /// <summary>Kullanıcının iptal edilmemiş cihazları (takipli).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Cihazlar (en son eşlenen önce).</returns>
    Task<IReadOnlyList<AgentDevice>> ListActiveAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Kullanıcının bu bilgisayar adlı iptal edilmemiş cihazı (takipli).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="machineName">Bilgisayar adı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Cihaz ya da <c>null</c>.</returns>
    Task<AgentDevice?> FindActiveByMachineAsync(Guid userId, string machineName, CancellationToken cancellationToken = default);

    /// <summary>Kullanıcının bu refresh token ailesine bağlı iptal edilmemiş cihazı (takipli).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="tokenFamilyId">Aile.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Cihaz ya da <c>null</c>.</returns>
    Task<AgentDevice?> FindActiveByTokenFamilyAsync(Guid userId, Guid tokenFamilyId, CancellationToken cancellationToken = default);
}
