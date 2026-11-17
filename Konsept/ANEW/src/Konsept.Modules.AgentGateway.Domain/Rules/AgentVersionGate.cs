namespace Konsept.Modules.AgentGateway.Domain.Rules;

/// <summary>
/// Sürüm kapısı (spec 0023 Req 4, 15): Web, API ve Ajan tek ürün versiyonunu paylaşır; ajanın sürümü sunucununkinden farklıysa
/// güncelleme gerekir. Karşılaştırma <c>+sha</c> ekinden arındırılmış sürümler arasında birebirdir. Kapı yapılandırmayla kapatılabilir
/// (<c>AgentGateway:RequireSameVersion</c>, varsayılan açık; yalnızca test kurulumu kapatır).
/// </summary>
/// <param name="serverVersion">Sunucunun ürün versiyonu.</param>
/// <param name="enabled">Kapı açık mı.</param>
public sealed class AgentVersionGate(string serverVersion, bool enabled = true)
{
    /// <summary>Gets sunucunun ürün versiyonu.</summary>
    public string ServerVersion { get; } = Normalize(serverVersion);

    /// <summary>Ajanın güncellenmesi gerekiyor mu.</summary>
    /// <param name="agentVersion">Ajanın bildirdiği sürüm.</param>
    /// <returns>Gerekiyorsa <c>true</c>.</returns>
    public bool RequiresUpdate(string? agentVersion) =>
        enabled && !string.Equals(Normalize(agentVersion), ServerVersion, StringComparison.Ordinal);

    private static string Normalize(string? version)
    {
        var value = (version ?? string.Empty).Trim();
        var plus = value.IndexOf('+', StringComparison.Ordinal);
        return plus < 0 ? value : value[..plus];
    }
}
