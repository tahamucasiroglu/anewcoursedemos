namespace TahaKonsept.Shared.Contracts.Identity;

/// <summary>
/// Yönetim/diagnostik listeleri için kullanıcı özeti (<c>IUserDirectory.ListAsync</c>).
/// </summary>
public sealed record UserSummaryDto
{
    /// <summary>Kullanıcı kimliği.</summary>
    public Guid Id { get; init; }

    /// <summary>Kullanıcı adı.</summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>Yönetici mi.</summary>
    public bool IsAdmin { get; init; }

    /// <summary>Yasaklı mı (giriş ve token yenileme kapalı).</summary>
    public bool IsBanned { get; init; }

    /// <summary>Hesabın oluşturulma anı (UTC).</summary>
    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>Son başarılı giriş anı (UTC); hiç giriş yoksa null.</summary>
    public DateTimeOffset? LastLoginAtUtc { get; init; }
}
