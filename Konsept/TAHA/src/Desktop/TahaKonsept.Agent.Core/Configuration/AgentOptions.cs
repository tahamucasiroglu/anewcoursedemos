using System.Reflection;

namespace TahaKonsept.Agent.Core.Configuration;

/// <summary>
/// Ajan çekirdeğinin başlangıç seçenekleri (<c>AddAgentCore(services, options)</c>). Kullanıcının değiştirebildiği,
/// diske yazılan değerler <see cref="AgentSettings"/>'tedir; burada yalnızca varsayılanlar ve teknik ayarlar bulunur.
/// Önemli üyeler: <see cref="DefaultServerBaseUrl"/>, <see cref="VideoServerPort"/>, <see cref="RootDirectory"/>, <see cref="AgentVersion"/>.
/// </summary>
public sealed class AgentOptions
{
    /// <summary>İlk açılışta kullanılacak API adresi (ayarlarda değiştirilebilir).</summary>
    public string DefaultServerBaseUrl { get; set; } = "http://192.168.1.102:7111";

    /// <summary>Yerel video sunucusunun varsayılan portu.</summary>
    public int VideoServerPort { get; set; } = 7120;

    /// <summary>Kök klasör (boşsa <c>%LocalAppData%/TahaKonsept</c>); testler geçici klasör verir.</summary>
    public string? RootDirectory { get; set; }

    /// <summary>Ajan sürümü (varsayılan: giriş derlemesinin sürümü).</summary>
    public string AgentVersion { get; set; } = ResolveDefaultVersion();

    /// <summary>Log dosyası öneki (<c>logs/agent-YYYYMMDD.log</c>).</summary>
    public string LogAppName { get; set; } = "agent";

    /// <summary>Serilog dosya loglaması eklensin mi (testlerde kapatılabilir).</summary>
    public bool EnableFileLogging { get; set; } = true;

    /// <summary>Aynı anda çalışan en fazla aşama sayısı (GPU/CPU yoğun olduğundan varsayılan 1).</summary>
    public int MaxConcurrentStages { get; set; } = 1;

    /// <summary>Hub canlılık sinyali aralığı.</summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Cihaz eşlemede <c>slow_down</c> yanıtı gelince yoklama aralığına eklenen süre.</summary>
    public TimeSpan PairingSlowDownStep { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Erişim token'ı bu süreden az kalmışsa önceden yenilenir.</summary>
    public TimeSpan TokenRefreshSkew { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>Ajan başlarken yerel video sunucusu açılsın mı.</summary>
    public bool StartVideoServer { get; set; } = true;

    private static string ResolveDefaultVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? typeof(AgentOptions).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return string.IsNullOrWhiteSpace(informational) ? assembly.GetName().Version?.ToString() ?? "0.0.0" : informational;
    }
}
