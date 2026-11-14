using Shouldly;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Diagnostics.Domain.Entities;
using TahaKonsept.Shared.Contracts.Diagnostics;
using Xunit;

namespace TahaKonsept.Modules.Diagnostics.Tests.ErrorReports;

/// <summary>
/// <see cref="ErrorReport.ChangeStatus"/> durum akışı: New → Seen → Resolved, New → Resolved, Resolved → New; diğerleri reddedilir.
/// </summary>
public sealed class ErrorReportStatusFlowTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 9, 0, 0, TimeSpan.Zero);

    /// <summary>Geçiş tablosu.</summary>
    /// <param name="from">Mevcut.</param>
    /// <param name="to">Hedef.</param>
    /// <param name="allowed">İzinli mi.</param>
    [Theory]
    [InlineData(ErrorReportStatus.New, ErrorReportStatus.Seen, true)]
    [InlineData(ErrorReportStatus.New, ErrorReportStatus.Resolved, true)]
    [InlineData(ErrorReportStatus.Seen, ErrorReportStatus.Resolved, true)]
    [InlineData(ErrorReportStatus.Resolved, ErrorReportStatus.New, true)]
    [InlineData(ErrorReportStatus.Seen, ErrorReportStatus.Seen, true)]
    [InlineData(ErrorReportStatus.Seen, ErrorReportStatus.New, false)]
    [InlineData(ErrorReportStatus.Resolved, ErrorReportStatus.Seen, false)]
    public void Transition_table(ErrorReportStatus from, ErrorReportStatus to, bool allowed)
    {
        ErrorReport.CanTransition(from, to).ShouldBe(allowed);
    }

    /// <summary>Tam akış: yeni → görüldü → çözüldü → yeniden aç; değişiklik anı kaydedilir.</summary>
    [Fact]
    public void Full_flow_records_change_time()
    {
        var report = NewReport();
        report.Status.ShouldBe(ErrorReportStatus.New);
        report.StatusChangedAtUtc.ShouldBeNull();

        report.ChangeStatus(ErrorReportStatus.Seen, Now).IsSuccess.ShouldBeTrue();
        report.ChangeStatus(ErrorReportStatus.Resolved, Now.AddMinutes(5)).IsSuccess.ShouldBeTrue();
        report.StatusChangedAtUtc.ShouldBe(Now.AddMinutes(5));
        report.ChangeStatus(ErrorReportStatus.New, Now.AddMinutes(9)).IsSuccess.ShouldBeTrue();
        report.Status.ShouldBe(ErrorReportStatus.New);
    }

    /// <summary>İzinsiz geçiş doğrulama hatası döner, durum değişmez.</summary>
    [Fact]
    public void Invalid_transition_keeps_status()
    {
        var report = NewReport();
        report.ChangeStatus(ErrorReportStatus.Seen, Now);

        var result = report.ChangeStatus(ErrorReportStatus.New, Now.AddMinutes(1));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Message.ShouldContain("Görüldü");
        report.Status.ShouldBe(ErrorReportStatus.Seen);
        report.StatusChangedAtUtc.ShouldBe(Now);
    }

    /// <summary>Tanımsız durum değeri reddedilir.</summary>
    [Fact]
    public void Undefined_status_is_rejected()
    {
        NewReport().ChangeStatus((ErrorReportStatus)99, Now).IsFailure.ShouldBeTrue();
    }

    /// <summary>Boş isteğe bağlı alanlar null olur, uzun açıklama kırpılır.</summary>
    [Fact]
    public void Create_normalizes_fields()
    {
        var report = ErrorReport.Create(Guid.Empty, ErrorReportSource.Installer, new string('a', 2500), "  ", "", null, " 0.1.0 ", "PC-1");

        report.Message.Length.ShouldBe(ErrorReport.MaxMessageLength);
        report.Details.ShouldBeNull();
        report.PageUrl.ShouldBeNull();
        report.AppVersion.ShouldBe("0.1.0");
        report.Machine.ShouldBe("PC-1");
    }

    private static ErrorReport NewReport() =>
        ErrorReport.Create(Guid.NewGuid(), ErrorReportSource.Web, "Konu önerisi boş liste döndü", null, "/Konseptler", "Chrome", "0.1.0", null);
}
