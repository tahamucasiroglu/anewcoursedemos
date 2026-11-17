using Konsept.Modules.Monitoring.Application.Base;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;
using Shouldly;

namespace Konsept.Modules.Monitoring.Tests;

/// <summary>Hata bildirimi kuralları (spec 0022 AC-13, AC-15, AC-16).</summary>
public sealed class ErrorReportRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 16, 12, 0, 0, TimeSpan.FromHours(3));

    /// <summary>Kaynak alanı yalnızca web / ajan / API değerlerinden biridir; sözleşme adları <c>web</c>, <c>agent</c>, <c>api</c>.</summary>
    [Fact]
    public void ErrorReportSource_OnlyWebAgentApi()
    {
        Enum.GetValues<ErrorReportSource>().ShouldBe([ErrorReportSource.Web, ErrorReportSource.Agent, ErrorReportSource.Api]);
        Enum.GetValues<ErrorReportSource>().Select(ErrorReportServiceBase.SourceName).ShouldBe(["web", "agent", "api"]);
        Enum.GetValues<ErrorReportStatus>().ShouldBe([ErrorReportStatus.New, ErrorReportStatus.Reviewing, ErrorReportStatus.Resolved, ErrorReportStatus.Ignored]); // plan 0025 A-2.
    }

    /// <summary>
    /// Ajan bildirimi "Yeni" başlar, zaman UTC'dir; özet ve log satırları maskelenir, en yeni 200 satır alınır; bileşen ve adım saklanır;
    /// makine adı için alan yoktur.
    /// </summary>
    [Fact]
    public void CreateAgent_MasksKeepsNewest200AndComponentStep()
    {
        var lines = Enumerable.Range(1, 250).Select(i => $"satır {i:000}").Append("anahtar sk-proj-LOGLINEKEY12345-l9z").ToList();

        var report = ErrorReport.CreateAgent(Guid.NewGuid(), Now, "1.0.0", "kurulum hatası sk-proj-SUMMARYKEY1234-s1z", "1.0.0", "Windows 11", "FFmpeg", "indirme", lines);

        report.Status.ShouldBe(ErrorReportStatus.New);
        report.Source.ShouldBe(ErrorReportSource.Agent);
        report.CreatedAt.Offset.ShouldBe(TimeSpan.Zero);
        report.Component.ShouldBe("FFmpeg");
        report.Step.ShouldBe("indirme");
        report.Attachments!.Split('\n').Length.ShouldBe(200);
        report.Attachments.ShouldNotContain("satır 051");
        (report.Description + report.Attachments).ShouldNotContain("KEY1234");
        typeof(ErrorReport).GetProperties().Select(p => p.Name).ShouldNotContain(n => n.Contains("Machine", StringComparison.Ordinal));
    }
}
