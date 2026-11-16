namespace Konsept.Agent.Domain.Entities;

/// <summary>
/// Ajanın ayarları (ajan klasöründeki <c>agentsettings.json</c>; kurulum ekranı 0009'da).
/// </summary>
/// <param name="ServerUrl">API adresi (ör. <c>http://&lt;pi&gt;:8001/</c>).</param>
/// <param name="SiteUrl">Sitenin adresi (tray "Siteye git").</param>
/// <param name="ReconnectDelay">Yeniden bağlanma denemeleri arasındaki bekleme.</param>
/// <param name="Render">Kısıtlı render sınırları (spec 0018); verilmezse yükleyicinin varsayılanları.</param>
public sealed record AgentSettings(Uri ServerUrl, Uri SiteUrl, TimeSpan ReconnectDelay, RenderLimits? Render = null);
