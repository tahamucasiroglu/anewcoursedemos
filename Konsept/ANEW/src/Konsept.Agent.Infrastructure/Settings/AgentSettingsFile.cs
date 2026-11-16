namespace Konsept.Agent.Infrastructure.Settings;

/// <summary>
/// <c>agentsettings.json</c> dosyasının biçimi.
/// </summary>
/// <param name="ServerUrl">API adresi.</param>
/// <param name="SiteUrl">Site adresi.</param>
/// <param name="ReconnectDelaySeconds">Yeniden bağlanma aralığı (sn).</param>
/// <param name="RenderSceneTimeoutSeconds">Sahne başına render süresi sınırı (sn; spec 0018).</param>
/// <param name="RenderMemoryMb">Render süreç ağacının bellek sınırı (MB).</param>
/// <param name="RenderMaxProcesses">Render'ın aynı anda en çok süreç sayısı.</param>
public sealed record AgentSettingsFile(
    string? ServerUrl,
    string? SiteUrl,
    double? ReconnectDelaySeconds,
    double? RenderSceneTimeoutSeconds = null,
    long? RenderMemoryMb = null,
    int? RenderMaxProcesses = null);
