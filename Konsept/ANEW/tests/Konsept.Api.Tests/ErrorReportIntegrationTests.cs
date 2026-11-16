using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Api.Tests.Support;
using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Gerçek PostgreSQL 17 ile hata bildirimleri (spec 0022 AC-1, AC-12, AC-13; full profil): tablo migration'la gelir, numara sıralı üretilir,
/// kaynak/durum ve ekler saklanır, saatlik sayım API kayıtlarını saymaz.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ErrorReportIntegrationTests : IClassFixture<IntegrationApiFactory>
{
    private readonly IntegrationApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="ErrorReportIntegrationTests"/> class.</summary>
    /// <param name="factory">Gerçek veritabanlı API fabrikası.</param>
    public ErrorReportIntegrationTests(IntegrationApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Web bildirimi gerçek veritabanında numara, kaynak, durum ve Günlük ekiyle saklanır; API kaydı sayıma girmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ErrorReport_RealDatabase_StoredWithNumber()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await AuthenticatedClient.CreateAsync(_factory, ct);
        var userId = (await client.GetFromJsonAsync<JsonElement>("/api/auth/session", ct)).GetProperty("userId").GetGuid();

        using var first = await client.PostAsJsonAsync("/api/error-reports", new { description = "gerçek veritabanı bildirimi 1", page = "/" }, ct);
        using var second = await client.PostAsJsonAsync("/api/error-reports", new { description = "gerçek veritabanı bildirimi 2", page = "/ayarlar", correlationId = "corr-db" }, ct);
        var n1 = (await first.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("number").GetInt64();
        var n2 = (await second.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("number").GetInt64();

        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IErrorReportRepository>();
        await repository.AddAsync(ErrorReport.CreateApi(userId, DateTimeOffset.UtcNow, "1", "api hatası", "corr-api"), ct);
        var stored = await repository.GetByNumberAsync(n2, ct);
        var count = await repository.CountUserReportsSinceAsync(userId, DateTimeOffset.UtcNow.AddHours(-1), ct);

        n2.ShouldBeGreaterThan(n1);
        stored.ShouldNotBeNull();
        stored.Source.ShouldBe(ErrorReportSource.Web);
        stored.Status.ShouldBe(ErrorReportStatus.New);
        stored.UserId.ShouldBe(userId);
        stored.Page.ShouldBe("/ayarlar");
        stored.CorrelationId.ShouldBe("corr-db");
        stored.Attachments!.ShouldContain("Hata bildirimi gönderildi"); // ilk bildirimin Günlük kaydı ikincinin ekinde
        count.ShouldBe(2);
    }
}
