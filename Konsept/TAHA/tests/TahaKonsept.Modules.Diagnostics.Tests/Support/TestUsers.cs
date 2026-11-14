using NSubstitute;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Diagnostics.Application.Common;

namespace TahaKonsept.Modules.Diagnostics.Tests.Support;

/// <summary>
/// <see cref="ICurrentUser"/> sahteleri: <see cref="Admin"/>, <see cref="Regular"/>, <see cref="Anonymous"/>.
/// </summary>
public static class TestUsers
{
    /// <summary>Yönetici (admin.access izni).</summary>
    /// <param name="id">Kimlik.</param>
    /// <returns>Sahte kullanıcı.</returns>
    public static ICurrentUser Admin(Guid id) => Create(id, true);

    /// <summary>Normal kullanıcı.</summary>
    /// <param name="id">Kimlik.</param>
    /// <returns>Sahte kullanıcı.</returns>
    public static ICurrentUser Regular(Guid id) => Create(id, false);

    /// <summary>Oturumsuz.</summary>
    /// <returns>Sahte kullanıcı.</returns>
    public static ICurrentUser Anonymous()
    {
        var user = Substitute.For<ICurrentUser>();
        user.UserId.Returns((Guid?)null);
        user.IsAuthenticated.Returns(false);
        user.HasPermission(Arg.Any<string>()).Returns(false);
        return user;
    }

    private static ICurrentUser Create(Guid id, bool admin)
    {
        var user = Substitute.For<ICurrentUser>();
        user.UserId.Returns(id);
        user.IsAuthenticated.Returns(true);
        user.HasPermission(DiagnosticsPermissions.AdminAccess).Returns(admin);
        return user;
    }
}
