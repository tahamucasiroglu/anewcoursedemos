namespace TahaKonsept.Shared.Contracts.Diagnostics;

/// <summary>
/// Yönetim panelindeki kullanıcı satırı.
/// </summary>
public sealed record AdminUserDto
{
    /// <summary>Kullanıcı kimliği.</summary>
    public Guid Id { get; init; }

    /// <summary>Kullanıcı adı.</summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>Yönetici mi.</summary>
    public bool IsAdmin { get; init; }

    /// <summary>Yasaklı mı.</summary>
    public bool IsBanned { get; init; }

    /// <summary>Kayıt anı (UTC) ya da <c>null</c>.</summary>
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>Son giriş anı (UTC) ya da <c>null</c>.</summary>
    public DateTimeOffset? LastLoginAt { get; init; }
}
