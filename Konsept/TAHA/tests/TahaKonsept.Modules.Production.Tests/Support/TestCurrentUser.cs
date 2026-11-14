using TahaKonsept.BuildingBlocks.Application.Abstract;

namespace TahaKonsept.Modules.Production.Tests.Support;

/// <summary>
/// Sabit kimlikli oturum kullanıcısı (testler).
/// </summary>
/// <param name="userId">Kullanıcı.</param>
public sealed class TestCurrentUser(Guid userId) : ICurrentUser
{
    /// <inheritdoc />
    public Guid? UserId => userId;

    /// <inheritdoc />
    public string? UserName => "taha";

    /// <inheritdoc />
    public bool IsAuthenticated => true;

    /// <inheritdoc />
    public IReadOnlyCollection<string> Permissions => [];

    /// <inheritdoc />
    public bool HasPermission(string permission) => true;

    /// <inheritdoc />
    public Guid GetRequiredUserId() => userId;
}
