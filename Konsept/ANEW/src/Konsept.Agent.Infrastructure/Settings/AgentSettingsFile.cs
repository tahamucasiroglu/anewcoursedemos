namespace Konsept.Agent.Infrastructure.Settings;

/// <summary>
/// <c>agentsettings.json</c> dosyasının biçimi.
/// </summary>
/// <param name="ServerUrl">API adresi.</param>
/// <param name="SiteUrl">Site adresi.</param>
/// <param name="ReconnectDelaySeconds">Yeniden bağlanma aralığı (sn).</param>
public sealed record AgentSettingsFile(string? ServerUrl, string? SiteUrl, double? ReconnectDelaySeconds);
