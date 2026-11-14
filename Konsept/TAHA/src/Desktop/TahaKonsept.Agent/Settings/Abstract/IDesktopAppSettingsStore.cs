namespace TahaKonsept.Agent.Settings.Abstract;

/// <summary>Masaüstü uygulaması ayarlarını (<see cref="DesktopAppSettings"/>) okur ve kalıcı olarak yazar.</summary>
public interface IDesktopAppSettingsStore
{
    /// <summary>Güncel ayarlar.</summary>
    DesktopAppSettings Current { get; }

    /// <summary>Ayarları değiştirip kaydeder.</summary>
    /// <param name="change">Eskiden yeniyi üreten fonksiyon.</param>
    /// <returns>Kaydedilen ayarlar.</returns>
    DesktopAppSettings Update(Func<DesktopAppSettings, DesktopAppSettings> change);
}
