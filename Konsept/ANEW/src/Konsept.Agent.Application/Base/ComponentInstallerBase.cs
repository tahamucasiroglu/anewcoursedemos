using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Enums;
using Konsept.Modules.AgentGateway.Contracts.Dtos;

namespace Konsept.Agent.Application.Base;

/// <summary>
/// <see cref="IComponentInstaller"/> (spec 0027): bilgi ve kurulum <see cref="ISetupCoordinator"/>'dan; bekleme alt sınıfta (testte sahte saat).
/// Sunucu kullanıcı başına tek çalışan iş yürüttüğü için aynı anda tek kurulum komutu gelir.
/// </summary>
/// <param name="setup">Kurulum motoru.</param>
public abstract class ComponentInstallerBase(ISetupCoordinator setup) : IComponentInstaller
{
    /// <summary>Süren ilk kurulumu bekleme adımı.</summary>
    public static readonly TimeSpan WaitStep = TimeSpan.FromMilliseconds(500);

    private readonly ISetupCoordinator _setup = setup ?? throw new ArgumentNullException(nameof(setup));

    /// <inheritdoc />
    public IReadOnlyList<AgentComponentInfo> Describe(IEnumerable<string> componentIds)
    {
        ArgumentNullException.ThrowIfNull(componentIds);
        var wanted = componentIds.ToHashSet(StringComparer.Ordinal);
        return [.. _setup.Components
            .Select(s => s.Component)
            .Where(c => wanted.Contains(c.Id))
            .Select(c => new AgentComponentInfo(c.Id, c.Name, c.DownloadBytes, c.InstalledBytes))];
    }

    /// <inheritdoc />
    public async Task<AgentInstallReply> InstallAsync(AgentInstallRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ids = (request.Components ?? []).Distinct(StringComparer.Ordinal).ToList();
        var known = _setup.Components.Select(s => s.Component.Id).ToHashSet(StringComparer.Ordinal);
        var unknown = ids.Where(id => !known.Contains(id)).ToList();
        if (ids.Count == 0 || unknown.Count > 0)
        {
            return new AgentInstallReply(false, unknown); // Listede olmayan hiçbir şey kurulmaz (Req 8).
        }

        // İlk kurulum (ya da pencereden Tekrar dene) sürüyorsa bitmesi beklenir; başlatma anındaki yarışta kurulum çalışmadıysa yeniden denenir.
        while (!await _setup.InstallAsync(ids, cancellationToken).ConfigureAwait(false))
        {
            await DelayAsync(WaitStep, cancellationToken).ConfigureAwait(false);
        }
        var failed = _setup.Components
            .Where(s => ids.Contains(s.Component.Id, StringComparer.Ordinal) && s.Status != ComponentStatus.Installed)
            .Select(s => s.Component.Id)
            .ToList();
        return new AgentInstallReply(failed.Count == 0, failed);
    }

    /// <summary>Bekler.</summary>
    /// <param name="delay">Süre.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected abstract Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}
