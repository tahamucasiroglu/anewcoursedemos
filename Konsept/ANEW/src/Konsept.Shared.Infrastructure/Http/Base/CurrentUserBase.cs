using System.Security.Claims;
using Konsept.Shared.Kernel.Abstract;

namespace Konsept.Shared.Infrastructure.Http.Base;

/// <summary>
/// İsteği yapan kullanıcının ortak çözümü: <c>sub</c> ve <c>sid</c> claim'lerini okur; kimlik doğrulanmamışsa ya da
/// claim geçersizse <c>null</c> döner. Kimliğin kaynağı (HTTP bağlamı) alt sınıftadır.
/// </summary>
public abstract class CurrentUserBase : ICurrentUser
{
    /// <summary>Kullanıcı kimliği claim'i.</summary>
    public const string SubjectClaim = "sub";

    /// <summary>Oturum kimliği claim'i.</summary>
    public const string SessionClaim = "sid";

    /// <inheritdoc />
    public Guid? UserId => ReadGuid(SubjectClaim);

    /// <inheritdoc />
    public Guid? SessionId => ReadGuid(SessionClaim);

    /// <inheritdoc />
    public bool IsAuthenticated => UserId is not null;

    /// <summary>Gets isteğin kimliği; yoksa <c>null</c>.</summary>
    protected abstract ClaimsPrincipal? Principal { get; }

    private Guid? ReadGuid(string claim)
    {
        var principal = Principal;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return Guid.TryParse(principal.FindFirst(claim)?.Value, out var id) ? id : null;
    }
}
