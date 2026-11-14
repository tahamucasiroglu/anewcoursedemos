using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Startup.Abstract;

/// <summary>
/// "Windows ile başlat" ayarı: uygulamayı oturum açılışında (<c>--tray</c> argümanıyla, pencere açmadan) başlatma kaydı.
/// </summary>
public interface IStartupRegistration
{
    /// <summary>Kayıt etkin mi.</summary>
    bool IsEnabled { get; }

    /// <summary>Kaydı açar/kapatır.</summary>
    /// <param name="enabled">Açılsın mı.</param>
    /// <returns>Sonuç.</returns>
    Result SetEnabled(bool enabled);
}
