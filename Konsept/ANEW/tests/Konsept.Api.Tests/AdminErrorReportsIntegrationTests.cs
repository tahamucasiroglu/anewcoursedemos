using Konsept.Api.Tests.Support;
using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Gerçek PostgreSQL 17 ile admin hata bildirimi sorguları (spec 0025 AC-2, AC-6; full profil): migration'la gelen not/an sütunları,
/// kaynak + durum filtresi ve numaraya göre en yeni üstte sıra, tek ifadelik durum güncellemesi (yok → false) ve açık sayısı
/// (Yeni + İnceleniyor). Veritabanı paylaşıldığı için yalnızca bu testin yazdığı kayıtlar ve farklar üzerinden assert edilir.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AdminErrorReportsIntegrationTests : IClassFixture<IntegrationApiFactory>
{
    private readonly IntegrationApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="AdminErrorReportsIntegrationTests"/> class.</summary>
    /// <param name="factory">Gerçek veritabanlı API fabrikası.</param>
    public AdminErrorReportsIntegrationTests(IntegrationApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Liste filtreleri, sıra, durum + not güncellemesi ve açık sayısı gerçek SQL ile doğrudur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AdminReports_RealDatabase_FilterOrderUpdateOpenCount()
    {
        var ct = TestContext.Current.CancellationToken;
        var at = new DateTimeOffset(2026, 11, 14, 9, 0, 0, TimeSpan.Zero);
        await using var scope = _factory.Services.CreateAsyncScope();
        var reports = scope.ServiceProvider.GetRequiredService<IErrorReportRepository>();
        var admin = scope.ServiceProvider.GetRequiredService<IAdminActivityRepository>();
        var openBefore = await admin.CountOpenErrorReportsAsync(ct);
        var web = ErrorReport.CreateWeb(Guid.NewGuid(), at, "1", "entegrasyon web bildirimi", "/", null, []);
        var agent = ErrorReport.CreateAgent(Guid.NewGuid(), at, "1", "entegrasyon ajan hatası", "0.5.0", null, null, null, []);
        var api = ErrorReport.CreateApi(null, at, "1", "entegrasyon api hatası", "corr-int");
        await reports.AddAsync(web, ct);
        await reports.AddAsync(agent, ct);
        await reports.AddAsync(api, ct);
        var openAfterAdd = await admin.CountOpenErrorReportsAsync(ct);

        (await reports.UpdateStatusAsync(agent.Number, ErrorReportStatus.Reviewing, "inceleniyor notu", at.AddHours(1), ct)).ShouldBeTrue();
        (await reports.UpdateStatusAsync(web.Number, ErrorReportStatus.Resolved, null, at.AddHours(2), ct)).ShouldBeTrue();
        (await reports.UpdateStatusAsync(long.MaxValue, ErrorReportStatus.Resolved, null, at, ct)).ShouldBeFalse();
        var openAfterChange = await admin.CountOpenErrorReportsAsync(ct);
        var storedAgent = await reports.GetByNumberAsync(agent.Number, ct);
        var reviewingAgents = await reports.ListAsync(ErrorReportSource.Agent, [ErrorReportStatus.Reviewing], 0, 1000, ct);
        var resolvedWeb = await reports.ListAsync(ErrorReportSource.Web, [ErrorReportStatus.Resolved], 0, 1000, ct);
        var openApi = await reports.ListAsync(ErrorReportSource.Api, [ErrorReportStatus.New, ErrorReportStatus.Reviewing], 0, 1000, ct);
        var all = await reports.ListAsync(null, null, 0, 1000, ct);
        var firstTwo = await reports.ListAsync(null, null, 0, 2, ct);

        openAfterAdd.ShouldBe(openBefore + 3);
        openAfterChange.ShouldBe(openBefore + 2); // Çözüldü düşer, İnceleniyor açık kalır.
        storedAgent.ShouldNotBeNull();
        storedAgent.Status.ShouldBe(ErrorReportStatus.Reviewing);
        storedAgent.AdminNote.ShouldBe("inceleniyor notu");
        storedAgent.StatusChangedAt.ShouldBe(at.AddHours(1));
        reviewingAgents.Items.Select(r => r.Number).ShouldContain(agent.Number);
        reviewingAgents.Items.ShouldAllBe(r => r.Source == ErrorReportSource.Agent && r.Status == ErrorReportStatus.Reviewing);
        resolvedWeb.Items.Select(r => r.Number).ShouldContain(web.Number);
        resolvedWeb.Items.ShouldAllBe(r => r.Source == ErrorReportSource.Web && r.Status == ErrorReportStatus.Resolved);
        openApi.Items.Select(r => r.Number).ShouldContain(api.Number);
        openApi.Items.Select(r => r.Number).ShouldNotContain(web.Number);
        all.Total.ShouldBeGreaterThanOrEqualTo(3);
        all.Items.Select(r => r.Number).ShouldBe(all.Items.Select(r => r.Number).OrderDescending());
        firstTwo.Items.Count.ShouldBe(2);
        firstTwo.Total.ShouldBe(all.Total);
    }
}
