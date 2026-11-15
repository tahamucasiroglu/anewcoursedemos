namespace Konsept.Modules.Identity.Application.Dtos;

/// <summary>Geçerli oturumun kullanıcısı (<c>GET api/auth/session</c>).</summary>
/// <param name="UserId">Kullanıcı.</param>
/// <param name="Name">Ad.</param>
/// <param name="Email">E-posta.</param>
public sealed record CurrentUserDto(Guid UserId, string Name, string Email)
{
    /// <summary>
    /// Gets kullanıcının o anki yetkileri (spec 0011; rolden, her istekte veritabanından). Web arayüzde gizleme için kullanır;
    /// asıl karar Api'dedir.
    /// </summary>
    public IReadOnlyList<string> Permissions { get; init; } = [];
}
