using TahaKonsept.BuildingBlocks.Application.Abstract;

namespace TahaKonsept.Modules.Providers.Tests.Support;

/// <summary>
/// Sabit oturum kullanıcısı.
/// </summary>
public sealed class TestCurrentUser : ICurrentUser
{
    /// <summary>Kurucu.</summary>
    /// <param name="userId">Kullanıcı (null = yeni).</param>
    public TestCurrentUser(Guid? userId = null)
    {
        UserId = userId ?? Guid.CreateVersion7();
    }

    /// <inheritdoc />
    public Guid? UserId { get; }

    /// <inheritdoc />
    public string? UserName => "taha";

    /// <inheritdoc />
    public bool IsAuthenticated => true;

    /// <inheritdoc />
    public IReadOnlyCollection<string> Permissions => [];

    /// <inheritdoc />
    public bool HasPermission(string permission) => true;

    /// <inheritdoc />
    public Guid GetRequiredUserId() => UserId!.Value;
}
