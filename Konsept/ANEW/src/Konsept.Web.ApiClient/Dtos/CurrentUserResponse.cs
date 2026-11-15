namespace Konsept.Web.ApiClient.Dtos;

/// <summary>Geçerli oturumun kullanıcısı.</summary>
/// <param name="UserId">Kullanıcı.</param>
/// <param name="Name">Ad.</param>
/// <param name="Email">E-posta.</param>
public sealed record CurrentUserResponse(Guid UserId, string Name, string Email)
{
    /// <summary>
    /// Gets kullanıcının o anki yetkileri (spec 0011; Api her istekte rolden kurar). Web principal'ı her istekte bu listeyle
    /// yeniden kurar; yalnızca arayüzde gizleme ve Web politikası içindir, asıl karar Api'dedir.
    /// </summary>
    public IReadOnlyList<string> Permissions { get; init; } = [];
}
