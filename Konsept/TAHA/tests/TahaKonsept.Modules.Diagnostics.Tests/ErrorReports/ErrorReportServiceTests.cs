using NSubstitute;
using Shouldly;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Modules.Diagnostics.Application.Concrete;
using TahaKonsept.Modules.Diagnostics.Application.Models;
using TahaKonsept.Modules.Diagnostics.Infrastructure.Persistence;
using TahaKonsept.Modules.Diagnostics.Infrastructure.Persistence.Repositories;
using TahaKonsept.Modules.Diagnostics.Tests.Support;
using TahaKonsept.Shared.Contracts.Diagnostics;
using Xunit;

namespace TahaKonsept.Modules.Diagnostics.Tests.ErrorReports;

/// <summary>
/// <see cref="ErrorReportService"/>: gönderim (sürüm doldurma, günlüğe uyarı, anonim), yönetici listesi ve durum değişikliği.
/// </summary>
public sealed class ErrorReportServiceTests : IDisposable
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 14, 9, 0, 0, TimeSpan.Zero));
    private readonly DiagnosticsDbContext _db;
    private readonly IActivityLogger _activity = Substitute.For<IActivityLogger>();
    private readonly Guid _admin = Guid.NewGuid();

    /// <summary>Oluşturur.</summary>
    public ErrorReportServiceTests()
    {
        _db = TestDb.Create(_clock);
    }

    /// <summary>Kaynakları bırakır.</summary>
    public void Dispose() => _db.Dispose();

    /// <summary>Gönderim: sürüm boşsa sunucu sürümü, durum New, günlüğe uyarı düşer.</summary>
    [Fact]
    public async Task Create_fills_version_and_logs_warning()
    {
        var user = Guid.NewGuid();
        var result = await Service(TestUsers.Regular(user)).CreateAsync(
            new ErrorReportCreateRequest { Message = "Önizleme açılmadı", PageUrl = "/Uretilenler", UserAgent = "Firefox" },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(ErrorReportStatus.New);
        result.Value.UserId.ShouldBe(user);
        result.Value.AppVersion.ShouldNotBeNullOrWhiteSpace();
        await _activity.Received(1).WarningAsync(
            ActivitySources.Diagnostics,
            Arg.Is<string>(m => m.Contains("Önizleme açılmadı")),
            Arg.Any<object?>(),
            user,
            Arg.Any<CancellationToken>());
    }

    /// <summary>Oturumsuz kurulum ekranı da bildirim gönderebilir (sahip boş Guid).</summary>
    [Fact]
    public async Task Anonymous_installer_can_report()
    {
        var result = await Service(TestUsers.Anonymous()).CreateAsync(
            new ErrorReportCreateRequest { Source = ErrorReportSource.Installer, Message = "ffmpeg indirilemedi", Machine = "LAPTOP", AppVersion = "0.3.0" },
            TestContext.Current.CancellationToken);

        result.Value.UserId.ShouldBe(Guid.Empty);
        result.Value.AppVersion.ShouldBe("0.3.0");
    }

    /// <summary>Yönetici durumu değiştirir; geçersiz geçiş ve bulunamayan kayıt uygun hata döner.</summary>
    [Fact]
    public async Task Admin_changes_status_through_flow()
    {
        var service = Service(TestUsers.Admin(_admin));
        var created = (await service.CreateAsync(new ErrorReportCreateRequest { Message = "Hata" }, TestContext.Current.CancellationToken)).Value;

        var seen = await service.ChangeStatusAsync(created.Id, new ErrorReportStatusUpdateRequest { Status = ErrorReportStatus.Seen }, TestContext.Current.CancellationToken);
        seen.Value.Status.ShouldBe(ErrorReportStatus.Seen);
        seen.Value.StatusChangedAt.ShouldBe(_clock.UtcNow);

        var back = await service.ChangeStatusAsync(created.Id, new ErrorReportStatusUpdateRequest { Status = ErrorReportStatus.New }, TestContext.Current.CancellationToken);
        back.Error.Type.ShouldBe(ErrorType.Validation);

        var resolved = await service.ChangeStatusAsync(created.Id, new ErrorReportStatusUpdateRequest { Status = ErrorReportStatus.Resolved }, TestContext.Current.CancellationToken);
        resolved.Value.Status.ShouldBe(ErrorReportStatus.Resolved);

        var missing = await service.ChangeStatusAsync(Guid.NewGuid(), new ErrorReportStatusUpdateRequest { Status = ErrorReportStatus.Seen }, TestContext.Current.CancellationToken);
        missing.Error.Type.ShouldBe(ErrorType.NotFound);

        var open = await service.ListAsync(new ErrorReportQuery { Status = ErrorReportStatus.Resolved }, TestContext.Current.CancellationToken);
        open.Value.Items.ShouldHaveSingleItem().Id.ShouldBe(created.Id);
    }

    /// <summary>Normal kullanıcı listeleyemez ve durum değiştiremez.</summary>
    [Fact]
    public async Task Non_admin_cannot_list_or_change_status()
    {
        var service = Service(TestUsers.Regular(Guid.NewGuid()));
        var created = (await service.CreateAsync(new ErrorReportCreateRequest { Message = "Hata" }, TestContext.Current.CancellationToken)).Value;

        (await service.ListAsync(new ErrorReportQuery(), TestContext.Current.CancellationToken)).Error.Type.ShouldBe(ErrorType.Forbidden);
        (await service.ChangeStatusAsync(created.Id, new ErrorReportStatusUpdateRequest { Status = ErrorReportStatus.Resolved }, TestContext.Current.CancellationToken))
            .Error.Type.ShouldBe(ErrorType.Forbidden);
    }

    private ErrorReportService Service(TahaKonsept.BuildingBlocks.Application.Abstract.ICurrentUser user) =>
        new(new ErrorReportRepository(_db), _activity, _clock, user);
}
