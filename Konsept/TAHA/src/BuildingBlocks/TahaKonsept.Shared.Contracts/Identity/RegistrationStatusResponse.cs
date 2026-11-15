namespace TahaKonsept.Shared.Contracts.Identity;

/// <summary>
/// Kayıt durumu (<c>GET api/identity/registration/status</c>, anonim): <see cref="RegistrationOpen"/> yeni hesap açılabiliyorsa true
/// (yönetici panelinden kapatılabilir; varsayılan açık).
/// </summary>
public sealed record RegistrationStatusResponse
{
    /// <summary>Yeni hesap açılabilir mi.</summary>
    public bool RegistrationOpen { get; init; }
}
