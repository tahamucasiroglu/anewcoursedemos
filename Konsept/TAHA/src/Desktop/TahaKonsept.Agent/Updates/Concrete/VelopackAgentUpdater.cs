using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Updates.Base;
using TahaKonsept.Agent.WebSite.Abstract;
using Velopack;

namespace TahaKonsept.Agent.Updates.Concrete;

/// <summary>
/// Velopack güncelleyici: kaynak web arayüzünün <c>downloads/</c> klasörüdür (<c>releases.win.json</c> + <c>*.nupkg</c>,
/// <c>deploy/agent/pack-agent.ps1</c> oraya kopyalar). Yeni sürüm arka planda indirilir, uygulama kapanınca sessizce kurulur.
/// </summary>
public sealed class VelopackAgentUpdater : AgentUpdaterBase
{
    private readonly IWebSiteLocator _webSite;
    private UpdateManager? _manager;
    private UpdateInfo? _pending;

    /// <summary>Oluşturur.</summary>
    /// <param name="webSite">Web adresi.</param>
    /// <param name="logger">Log.</param>
    public VelopackAgentUpdater(IWebSiteLocator webSite, ILogger<VelopackAgentUpdater> logger)
        : base(logger)
    {
        _webSite = webSite;
    }

    /// <inheritdoc />
    protected override bool CanUpdate() => GetManager().IsInstalled;

    /// <inheritdoc />
    protected override async Task<bool> DownloadIfNewerAsync(CancellationToken ct)
    {
        var manager = GetManager();
        var info = await manager.CheckForUpdatesAsync().ConfigureAwait(false);
        if (info is null)
        {
            Logger.LogInformation("Uygulama güncel.");
            return false;
        }

        Logger.LogInformation("Yeni sürüm bulundu: {Version}; indiriliyor.", info.TargetFullRelease.Version);
        await manager.DownloadUpdatesAsync(info, null, ct).ConfigureAwait(false);
        _pending = info;
        return true;
    }

    /// <inheritdoc />
    protected override void ScheduleApply()
    {
        if (_pending is null)
        {
            return;
        }

        GetManager().WaitExitThenApplyUpdates(_pending.TargetFullRelease, silent: true, restart: false);
    }

    private UpdateManager GetManager() => _manager ??= new UpdateManager(new Uri(new Uri(_webSite.GetWebUrl()), "downloads/").AbsoluteUri);
}
