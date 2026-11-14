using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Base;

namespace TahaKonsept.Modules.Agents.Application.Concrete;

/// <summary>
/// Ajan oturum iptalcisi: kullanıcının tüm cihazlarını iptal eder (yeniden eşleme gerekir) ve hub bağlantılarını keser.
/// Identity'nin "tüm oturumlardan çık" adaptörü bunu çağırır.
/// </summary>
public sealed class AgentSessionRevoker : AgentsServiceBase, IAgentSessionRevoker
{
    private readonly IAgentDeviceRepository _devices;
    private readonly IAgentConnectionTracker _tracker;

    /// <summary>Oluşturur.</summary>
    /// <param name="devices">Cihaz deposu.</param>
    /// <param name="tracker">Bağlantı izleyici.</param>
    /// <param name="clock">Saat.</param>
    public AgentSessionRevoker(IAgentDeviceRepository devices, IAgentConnectionTracker tracker, IClock clock)
        : base(clock)
    {
        _devices = devices ?? throw new ArgumentNullException(nameof(devices));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
    }

    /// <summary>Kullanıcının tüm ajanlarını iptal eder.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İptal edilen cihaz sayısı.</returns>
    public async Task<int> RevokeUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var devices = await _devices.ListActiveAsync(userId, cancellationToken);
        var now = Now;
        foreach (var device in devices)
        {
            device.Revoke(now);
        }

        if (devices.Count > 0)
        {
            await _devices.UnitOfWork.SaveChangesAsync(cancellationToken);
        }

        foreach (var connection in _tracker.GetByUser(userId))
        {
            connection.Abort();
        }

        return devices.Count;
    }

    /// <summary>Aileye bağlı cihazı iptal eder.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="tokenFamilyId">Aile.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İptal edildiyse <c>true</c>.</returns>
    public async Task<bool> RevokeTokenFamilyAsync(Guid userId, Guid tokenFamilyId, CancellationToken cancellationToken = default)
    {
        var device = await _devices.FindActiveByTokenFamilyAsync(userId, tokenFamilyId, cancellationToken);
        if (device is null)
        {
            return false;
        }

        device.Revoke(Now);
        await _devices.UnitOfWork.SaveChangesAsync(cancellationToken);
        DisconnectDevice(device.Id);
        return true;
    }

    /// <summary>Cihazın bağlantılarını keser.</summary>
    /// <param name="deviceId">Cihaz.</param>
    public void DisconnectDevice(Guid deviceId)
    {
        foreach (var connection in _tracker.GetByDevice(deviceId))
        {
            connection.Abort();
        }
    }
}
