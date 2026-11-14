using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Settings.Base;

namespace TahaKonsept.Agent.Settings.Concrete;

/// <summary>Masaüstü ayarlarını <c>%LocalAppData%/TahaKonsept/Config/desktop-settings.json</c> dosyasında saklar.</summary>
public sealed class JsonDesktopAppSettingsStore : DesktopAppSettingsStoreBase
{
    /// <summary>Dosya adı.</summary>
    public const string FileName = "desktop-settings.json";

    /// <summary>Oluşturur.</summary>
    /// <param name="paths">Ajan klasörleri.</param>
    public JsonDesktopAppSettingsStore(IAgentPaths paths)
        : base(Path.Combine(paths.ConfigDir, FileName))
    {
    }
}
