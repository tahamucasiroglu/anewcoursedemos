namespace Konsept.Web.ApiClient.Dtos;

/// <summary>
/// Api'nin verdiği token çifti. Web bunları yalnızca Data Protection ile şifreli HttpOnly cookie'deki oturum biletinde tutar;
/// tarayıcı görmez (docs/security.md).
/// </summary>
/// <param name="AccessToken">JWT (15 dk).</param>
/// <param name="AccessTokenExpiresAt">JWT'nin bitişi (UTC).</param>
/// <param name="RefreshToken">Dönen refresh token.</param>
/// <param name="UserId">Kullanıcı.</param>
/// <param name="Name">Ad.</param>
public sealed record AuthSessionResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAt, string RefreshToken, Guid UserId, string Name);
