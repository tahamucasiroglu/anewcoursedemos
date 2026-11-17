namespace Konsept.Modules.Identity.Contracts.Dtos;

/// <summary>
/// Admin panelinin kullanıcı hakkında okuyabildiği en az bilgi (spec 0010 Req 3–5): kimlik, ad, e-posta, kayıt anı ve banlı mı
/// (spec 0024 Req 9, banlı kullanıcı kartı). Şifre, rol, oturum ya da kilit bilgisi içermez.
/// </summary>
/// <param name="Id">Kullanıcı.</param>
/// <param name="Name">Ad.</param>
/// <param name="Email">E-posta.</param>
/// <param name="RegisteredAt">Kayıt anı (UTC).</param>
/// <param name="IsBanned">Hesap banlı mı.</param>
public sealed record UserDirectoryEntry(Guid Id, string Name, string Email, DateTimeOffset RegisteredAt, bool IsBanned = false);
