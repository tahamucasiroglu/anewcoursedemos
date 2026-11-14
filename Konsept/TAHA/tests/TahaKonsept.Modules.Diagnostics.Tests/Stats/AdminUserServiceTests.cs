using NSubstitute;
using Shouldly;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Modules.Diagnostics.Application.Concrete;
using TahaKonsept.Modules.Diagnostics.Tests.Support;
using Xunit;

namespace TahaKonsept.Modules.Diagnostics.Tests.Stats;

/// <summary>
/// <see cref="AdminUserService"/>: dizin bağlı değilken davranış, kendini yasaklama engeli, başarılı yasakta günlük kaydı.
/// </summary>
public sealed class AdminUserServiceTests
{
    private readonly Guid _admin = Guid.NewGuid();
    private readonly IActivityLogger _activity = Substitute.For<IActivityLogger>();

    /// <summary>Yer tutucu dizinle liste boş, yasaklama "kullanılamıyor".</summary>
    [Fact]
    public async Task Unavailable_directory_gives_empty_list_and_failure_on_ban()
    {
        var service = new AdminUserService(new UnavailableAdminUserDirectory(), _activity, TestUsers.Admin(_admin));

        (await service.ListAsync(TestContext.Current.CancellationToken)).Value.ShouldBeEmpty();
        (await service.BanAsync(Guid.NewGuid(), TestContext.Current.CancellationToken)).Error.Code.ShouldBe("diagnostics.user_directory_unavailable");
    }

    /// <summary>Yönetici kendini yasaklayamaz.</summary>
    [Fact]
    public async Task Cannot_ban_self()
    {
        var directory = Substitute.For<IAdminUserDirectory>();
        directory.IsAvailable.Returns(true);
        var service = new AdminUserService(directory, _activity, TestUsers.Admin(_admin));

        var result = await service.BanAsync(_admin, TestContext.Current.CancellationToken);

        result.Error.Type.ShouldBe(ErrorType.Validation);
        await directory.DidNotReceive().SetBannedAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Başarılı yasak dizine iletilir ve günlüğe uyarı düşer; normal kullanıcı yasaklayamaz.</summary>
    [Fact]
    public async Task Ban_and_unban_are_forwarded_and_logged()
    {
        var target = Guid.NewGuid();
        var directory = Substitute.For<IAdminUserDirectory>();
        directory.IsAvailable.Returns(true);
        directory.SetBannedAsync(target, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        var service = new AdminUserService(directory, _activity, TestUsers.Admin(_admin));

        (await service.BanAsync(target, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await service.UnbanAsync(target, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        await directory.Received(1).SetBannedAsync(target, true, Arg.Any<CancellationToken>());
        await directory.Received(1).SetBannedAsync(target, false, Arg.Any<CancellationToken>());
        await _activity.Received(2).WarningAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());

        var regular = new AdminUserService(directory, _activity, TestUsers.Regular(Guid.NewGuid()));
        (await regular.BanAsync(target, TestContext.Current.CancellationToken)).Error.Type.ShouldBe(ErrorType.Forbidden);
    }
}
