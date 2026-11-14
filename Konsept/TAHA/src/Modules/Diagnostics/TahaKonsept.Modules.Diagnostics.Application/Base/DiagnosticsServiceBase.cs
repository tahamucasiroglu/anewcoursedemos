using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Diagnostics.Application.Common;
using TahaKonsept.Modules.Diagnostics.Domain.Errors;

namespace TahaKonsept.Modules.Diagnostics.Application.Base;

/// <summary>
/// Diagnostics servislerinin ortak tabanı: oturum kullanıcısı, yönetici kontrolü (<see cref="RequireAdmin"/>),
/// görünürlük kapsamı (<see cref="OwnerScope"/>: yönetici → hepsi, diğerleri → kendisi + sistem) ve sayfa boyutu sıkıştırma.
/// </summary>
public abstract class DiagnosticsServiceBase
{
    /// <summary>Oluşturur.</summary>
    /// <param name="currentUser">Oturum kullanıcısı.</param>
    protected DiagnosticsServiceBase(ICurrentUser currentUser)
    {
        CurrentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    /// <summary>Oturum kullanıcısı.</summary>
    protected ICurrentUser CurrentUser { get; }

    /// <summary>Kullanıcı yönetici mi (<c>admin.access</c>).</summary>
    protected bool IsAdmin => CurrentUser.IsAuthenticated && CurrentUser.HasPermission(DiagnosticsPermissions.AdminAccess);

    /// <summary>Görünürlük kapsamı: yönetici için <c>null</c> (hepsi), diğerleri için kendi kimliği (oturumsuzsa <see cref="Guid.Empty"/>).</summary>
    protected Guid? OwnerScope => IsAdmin ? null : CurrentUser.UserId ?? Guid.Empty;

    /// <summary>Oturum kullanıcısının kimliği (oturumsuzsa <see cref="Guid.Empty"/>).</summary>
    protected Guid CurrentUserIdOrEmpty => CurrentUser.UserId ?? Guid.Empty;

    /// <summary>Yönetici değilse uygun hatayı döner, yönetici ise <c>null</c>.</summary>
    /// <returns>Hata ya da <c>null</c>.</returns>
    protected Error? RequireAdmin()
    {
        if (!CurrentUser.IsAuthenticated)
        {
            return DiagnosticsErrors.AuthenticationRequired;
        }

        return IsAdmin ? null : DiagnosticsErrors.AdminRequired;
    }

    /// <summary>Sayfa boyutunu 1–200 aralığına sıkıştırır.</summary>
    /// <param name="value">İstenen.</param>
    /// <param name="fallback">Boş/geçersizse.</param>
    /// <returns>Boyut.</returns>
    protected static int ClampTake(int? value, int fallback) => value is null or < 1 ? fallback : Math.Min(value.Value, 200);
}
