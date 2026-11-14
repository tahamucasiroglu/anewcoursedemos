namespace TahaKonsept.Shared.Contracts.Identity;

/// <summary>
/// Oturumdaki kullanıcının bilgisi (<c>GET api/identity/me</c>): kimlik, ad, yöneticilik, izinler ve tercihler.
/// </summary>
public sealed record CurrentUserResponse
{
    /// <summary>Kullanıcı kimliği.</summary>
    public Guid Id { get; init; }

    /// <summary>Kullanıcı adı.</summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>Yönetici mi (tüm izinlere sahip).</summary>
    public bool IsAdmin { get; init; }

    /// <summary>Etkin izinler (ör. <c>models.use</c>); bkz. <see cref="PermissionNames"/>.</summary>
    public IReadOnlyList<string> Permissions { get; init; } = [];

    /// <summary>Kullanıcı tercihleri.</summary>
    public UserPreferencesDto Preferences { get; init; } = new();
}
