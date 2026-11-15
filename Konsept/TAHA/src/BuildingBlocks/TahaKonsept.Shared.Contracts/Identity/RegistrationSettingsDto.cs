namespace TahaKonsept.Shared.Contracts.Identity;

/// <summary>
/// Yönetici kayıt ayarı (<c>GET|PUT api/identity/admin/registration</c>, yalnız <c>admin.access</c>):
/// <see cref="RegistrationOpen"/> yeni kayıtlara izin verilip verilmediği (varsayılan açık).
/// </summary>
public sealed record RegistrationSettingsDto
{
    /// <summary>Yeni kayıtlara izin ver.</summary>
    public bool RegistrationOpen { get; init; } = true;
}
