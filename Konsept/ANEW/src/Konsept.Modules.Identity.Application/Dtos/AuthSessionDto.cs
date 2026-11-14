namespace Konsept.Modules.Identity.Application.Dtos;

/// <summary>
/// Giriş, kayıt ya da refresh sonucu: kısa ömürlü erişim token'ı ve dönen refresh token. Yalnızca Web'e döner; Web bunları
/// şifreli HttpOnly cookie'de tutar, tarayıcı görmez.
/// </summary>
/// <param name="AccessToken">JWT (15 dk).</param>
/// <param name="AccessTokenExpiresAt">JWT'nin bitiş zamanı (UTC).</param>
/// <param name="RefreshToken">Yeni refresh token (<c>knr_…</c>).</param>
/// <param name="UserId">Kullanıcı.</param>
/// <param name="Name">Kullanıcının adı.</param>
public sealed record AuthSessionDto(string AccessToken, DateTimeOffset AccessTokenExpiresAt, string RefreshToken, Guid UserId, string Name);
