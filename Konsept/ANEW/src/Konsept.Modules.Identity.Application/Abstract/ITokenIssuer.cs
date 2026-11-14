using Konsept.Modules.Identity.Application.Dtos;

namespace Konsept.Modules.Identity.Application.Abstract;

/// <summary>Kısa ömürlü erişim token'ı (JWT, 15 dk; <c>sub</c>, <c>sid</c>, <c>name</c>) üretir.</summary>
public interface ITokenIssuer
{
    /// <summary>Erişim token'ı üretir.</summary>
    /// <param name="userId">Kullanıcı (<c>sub</c>).</param>
    /// <param name="name">Ad (<c>name</c>).</param>
    /// <param name="sessionId">Oturum (<c>sid</c>).</param>
    /// <param name="now">Üretim zamanı.</param>
    /// <returns>Token ve bitiş zamanı.</returns>
    IssuedAccessToken Issue(Guid userId, string name, Guid sessionId, DateTimeOffset now);
}
