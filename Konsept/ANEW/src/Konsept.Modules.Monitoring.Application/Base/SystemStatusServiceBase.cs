using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Monitoring.Application.Base;

/// <summary>
/// Sistem durumunun bileşimi: veritabanı erişilemiyorsa son olay okunmaz ve durum yine 200 ile döner,
/// böylece site "DB erişilemiyor" gösterebilir.
/// </summary>
public abstract class SystemStatusServiceBase : ISystemStatusService
{
    /// <inheritdoc />
    public async Task<Result<SystemStatusDto>> GetStatusAsync(CancellationToken cancellationToken)
    {
        var databaseAvailable = await IsDatabaseAvailableAsync(cancellationToken).ConfigureAwait(false);
        var lastEvent = databaseAvailable ? await ReadLastEventAsync(cancellationToken).ConfigureAwait(false) : null;
        return Result<SystemStatusDto>.Success(new SystemStatusDto(ReadVersion(), databaseAvailable, ReadAgentStatus(), lastEvent));
    }

    /// <inheritdoc />
    public async Task<Result<HealthDto>> GetHealthAsync(CancellationToken cancellationToken)
    {
        var databaseAvailable = await IsDatabaseAvailableAsync(cancellationToken).ConfigureAwait(false);
        return Result<HealthDto>.Success(new HealthDto(ReadVersion(), "ok", databaseAvailable ? "ok" : "down"));
    }

    /// <summary>Veritabanı erişimini yoklar.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Erişilebilirse <c>true</c>.</returns>
    protected abstract Task<bool> IsDatabaseAvailableAsync(CancellationToken cancellationToken);

    /// <summary>Son olayı okur.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Son olay ya da <c>null</c>.</returns>
    protected abstract Task<SystemEventDto?> ReadLastEventAsync(CancellationToken cancellationToken);

    /// <summary>Ürün versiyonunu okur.</summary>
    /// <returns>Versiyon.</returns>
    protected abstract string ReadVersion();

    /// <summary>Ajan durumunu okur.</summary>
    /// <returns>Ajan durumu.</returns>
    protected abstract AgentStatusDto ReadAgentStatus();
}
