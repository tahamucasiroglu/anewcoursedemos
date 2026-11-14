using TahaKonsept.BuildingBlocks.Application.Abstract;

namespace TahaKonsept.Modules.Agents.Tests.Support;

/// <summary>
/// Testlerde değiştirilebilir oturum kullanıcısı (<see cref="UserId"/> <c>null</c> → oturum yok).
/// </summary>
public sealed class TestCurrentUser : ICurrentUser
{
    /// <summary>Kullanıcı kimliği.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Kullanıcı adı.</summary>
    public string? UserName => UserId is null ? null : "taha";

    /// <summary>Oturum var mı.</summary>
    public bool IsAuthenticated => UserId is not null;

    /// <summary>İzinler.</summary>
    public IReadOnlyCollection<string> Permissions { get; } = [];

    /// <summary>İzin kontrolü.</summary>
    /// <param name="permission">İzin.</param>
    /// <returns>Her zaman <c>true</c>.</returns>
    public bool HasPermission(string permission) => true;

    /// <summary>Kullanıcı kimliği (zorunlu).</summary>
    /// <returns>Kimlik.</returns>
    public Guid GetRequiredUserId() => UserId ?? throw new InvalidOperationException("Oturum yok.");
}
