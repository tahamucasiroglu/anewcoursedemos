using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Base;
using TahaKonsept.Modules.Agents.Domain.Errors;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Agents.Application.Concrete;

/// <summary>
/// Eşlenmiş cihaz listesi ve eşleme kaldırma. Çevrimiçi durumu canlı bağlantı izleyicisinden gelir.
/// Kaldırma: cihazı iptal eder, <see cref="IAgentTokenIssuer.RevokeAsync"/> ile token'ları iptal ettirir, açık bağlantıları keser.
/// </summary>
public sealed class AgentDeviceService : AgentsServiceBase, IAgentDeviceService
{
    private readonly IAgentDeviceRepository _devices;
    private readonly IAgentConnectionTracker _tracker;
    private readonly IAgentTokenIssuer _tokenIssuer;
    private readonly IAgentSessionRevoker _revoker;
    private readonly ICurrentUser _currentUser;

    /// <summary>Oluşturur.</summary>
    /// <param name="devices">Cihaz deposu.</param>
    /// <param name="tracker">Bağlantı izleyici.</param>
    /// <param name="tokenIssuer">Token üretici.</param>
    /// <param name="revoker">Oturum iptalcisi.</param>
    /// <param name="currentUser">Oturum kullanıcısı.</param>
    /// <param name="clock">Saat.</param>
    public AgentDeviceService(
        IAgentDeviceRepository devices,
        IAgentConnectionTracker tracker,
        IAgentTokenIssuer tokenIssuer,
        IAgentSessionRevoker revoker,
        ICurrentUser currentUser,
        IClock clock)
        : base(clock)
    {
        _devices = devices ?? throw new ArgumentNullException(nameof(devices));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _tokenIssuer = tokenIssuer ?? throw new ArgumentNullException(nameof(tokenIssuer));
        _revoker = revoker ?? throw new ArgumentNullException(nameof(revoker));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    /// <summary>Cihazları listeler.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Cihazlar.</returns>
    public async Task<Result<IReadOnlyList<AgentDeviceInfo>>> ListAsync(CancellationToken cancellationToken = default)
    {
        var user = RequireUser(_currentUser);
        if (user.IsFailure)
        {
            return user.Error;
        }

        var devices = await _devices.ListActiveAsync(user.Value, cancellationToken);
        IReadOnlyList<AgentDeviceInfo> items = devices.Select(d => ToInfo(d, _tracker.IsDeviceOnline(d.Id))).ToList();
        return Result.Success(items);
    }

    /// <summary>Eşlemeyi kaldırır.</summary>
    /// <param name="id">Cihaz.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public async Task<Result> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = RequireUser(_currentUser);
        if (user.IsFailure)
        {
            return user.Error;
        }

        var device = await _devices.GetByIdAsync(id, cancellationToken);
        if (device is null || device.UserId != user.Value || device.IsRevoked)
        {
            return AgentErrors.DeviceNotFound;
        }

        device.Revoke(Now);
        await _devices.UnitOfWork.SaveChangesAsync(cancellationToken);
        await _tokenIssuer.RevokeAsync(user.Value, device.TokenFamilyId, cancellationToken);
        _revoker.DisconnectDevice(id);
        return Result.Success();
    }
}
