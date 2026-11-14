using TahaKonsept.BuildingBlocks.Application.Abstract;

namespace TahaKonsept.Modules.Identity.Tests.Support;

/// <summary>
/// Test için oturumdaki kullanıcı: <see cref="UserId"/> elle ayarlanır (null → oturum yok).
/// </summary>
public sealed class FakeCurrentUser : ICurrentUser
{
    /// <summary>Kullanıcı kimliği.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Kullanıcı adı.</summary>
    public string? UserName { get; set; }

    /// <summary>Oturum var mı.</summary>
    public bool IsAuthenticated => UserId.HasValue;

    /// <summary>İzinler.</summary>
    public IReadOnlyCollection<string> Permissions { get; set; } = [];

    /// <summary>İzin kontrolü.</summary>
    /// <param name="permission">İzin.</param>
    /// <returns>Varsa true.</returns>
    public bool HasPermission(string permission) => Permissions.Contains(permission);

    /// <summary>Kimliği döner; yoksa hata fırlatır.</summary>
    /// <returns>Kimlik.</returns>
    public Guid GetRequiredUserId() => UserId ?? throw new InvalidOperationException("Oturum yok.");
}
