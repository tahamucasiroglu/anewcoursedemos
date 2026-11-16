namespace Konsept.Modules.Identity.Contracts.Dtos;

/// <summary>
/// Admin panelinin kullanıcı hakkında okuyabildiği en az bilgi (spec 0010 Req 3–5): kimlik, ad, e-posta, kayıt anı. Şifre, rol,
/// oturum ya da kilit bilgisi içermez.
/// </summary>
/// <param name="Id">Kullanıcı.</param>
/// <param name="Name">Ad.</param>
/// <param name="Email">E-posta.</param>
/// <param name="RegisteredAt">Kayıt anı (UTC).</param>
public sealed record UserDirectoryEntry(Guid Id, string Name, string Email, DateTimeOffset RegisteredAt);
