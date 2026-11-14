using TahaKonsept.Agent.Core.Auth.Abstract;
using TahaKonsept.Agent.Core.Prerequisites.Abstract;
using TahaKonsept.Agent.Navigation.Abstract;
using TahaKonsept.Agent.Settings.Abstract;

namespace TahaKonsept.Agent.Navigation.Base;

/// <summary>
/// <see cref="IWindowNavigator"/> tabanı: kurulum gerekip gerekmediğine karar verir (<see cref="IsSetupRequired"/>: kurulum bitmemiş,
/// zorunlu bileşen eksik ya da eşleme yok) ve <see cref="ShowDefault"/>'u buna göre yönlendirir. Pencere yaratma türetilen sınıftadır.
/// </summary>
public abstract class WindowNavigatorBase : IWindowNavigator
{
    private readonly IDesktopAppSettingsStore _settings;
    private readonly IPrerequisiteInstaller _prerequisites;
    private readonly IAgentAuthService _auth;

    /// <summary>Oluşturur.</summary>
    /// <param name="settings">Masaüstü ayarları.</param>
    /// <param name="prerequisites">Ön gereksinimler.</param>
    /// <param name="auth">Oturum.</param>
    protected WindowNavigatorBase(IDesktopAppSettingsStore settings, IPrerequisiteInstaller prerequisites, IAgentAuthService auth)
    {
        _settings = settings;
        _prerequisites = prerequisites;
        _auth = auth;
    }

    /// <inheritdoc />
    public abstract void ShowSetup(bool pairingOnly = false);

    /// <inheritdoc />
    public abstract void ShowStatus();

    /// <inheritdoc />
    public abstract void CloseAll();

    /// <inheritdoc />
    public void ShowDefault()
    {
        if (IsSetupRequired())
        {
            ShowSetup();
        }
        else
        {
            ShowStatus();
        }
    }

    /// <inheritdoc />
    public bool IsSetupRequired() => !_settings.Current.SetupCompleted || !_prerequisites.AreRequiredInstalled() || !_auth.IsPaired;
}
