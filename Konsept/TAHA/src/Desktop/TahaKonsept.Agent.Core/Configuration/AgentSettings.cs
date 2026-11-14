namespace TahaKonsept.Agent.Core.Configuration;

/// <summary>
/// Kullanıcıya özel, <c>ConfigDir/settings.json</c> dosyasında saklanan ajan ayarları: sunucu adresi, video sunucu portu,
/// kuruluma özel rastgele akış anahtarı (<c>?k=</c>) ve kurulum kimliği.
/// </summary>
public sealed record AgentSettings
{
    /// <summary>API taban adresi (ör. <c>http://192.168.1.102:7111</c>).</summary>
    public string ServerBaseUrl { get; init; } = string.Empty;

    /// <summary>Yerel video sunucusu portu.</summary>
    public int VideoServerPort { get; init; } = 7120;

    /// <summary>Video sunucusu akış anahtarı (kurulum başına rastgele).</summary>
    public string StreamKey { get; init; } = string.Empty;

    /// <summary>Kurulum kimliği.</summary>
    public Guid InstallId { get; init; }
}
