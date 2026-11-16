using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Api.Tests.Support;
using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;
using Konsept.Modules.Monitoring.Domain.Rules;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Gerçek PostgreSQL 17 ile Günlük geçmişi (spec 0021 AC-6, AC-8, AC-9; full profil): yeni sütunlar ve indeks migration'la gelir,
/// Istanbul gece yarısı sınırı, 400 gün önceki kayıt, 50'lik sayfa, "Hata" filtresi ve sahiplik gerçek sorgularla çalışır.
/// </summary>
[Trait("Category", "Integration")]
public sealed class JournalIntegrationTests : IClassFixture<IntegrationApiFactory>
{
    private readonly IntegrationApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="JournalIntegrationTests"/> class.</summary>
    /// <param name="factory">Gerçek veritabanlı API fabrikası.</param>
    public JournalIntegrationTests(IntegrationApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Gün sınırı, süresiz saklama, sayfa, filtre, süre/profil alanları ve sahiplik gerçek veritabanında.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Journal_RealDatabase_DaysPagingOwnership()
    {
        var ct = TestContext.Current.CancellationToken;
        using var owner = await AuthenticatedClient.CreateAsync(_factory, ct);
        using var other = await AuthenticatedClient.CreateAsync(_factory, ct);
        var ownerId = (await owner.GetFromJsonAsync<JsonElement>("/api/auth/session", ct)).GetProperty("userId").GetGuid();
        var day = JournalDay.DayOf(DateTimeOffset.UtcNow).AddDays(-400);
        var (from, to) = JournalDay.RangeOf(day);
        using (var scope = _factory.Services.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ISystemEventRepository>();
            await repository.AddAsync(SystemEvent.Create("t", EventLevel.Info, "önceki gün son saniye", from.AddSeconds(-1), ownerId), ct);
            await repository.AddAsync(SystemEvent.Create("t", EventLevel.Critical, "gün başı", from, ownerId), ct);
            for (var i = 1; i <= 51; i++)
            {
                await repository.AddAsync(SystemEvent.Create("t", EventLevel.Info, $"kayıt {i:00}", from.AddMinutes(i), ownerId), ct);
            }

            await repository.AddAsync(
                SystemEvent.Create("t", EventLevel.Error, "video", to.AddSeconds(-1), ownerId).WithDetails(to.AddSeconds(-58), to.AddSeconds(-1), Guid.NewGuid(), "Kanal"),
                ct);
        }

        var date = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var first = await owner.GetFromJsonAsync<JsonElement>($"/api/journal?date={date}", ct);
        var more = await owner.GetFromJsonAsync<JsonElement>($"/api/journal?date={date}&skip=50", ct);
        var errors = await owner.GetFromJsonAsync<JsonElement>($"/api/journal?date={date}&filter=error", ct);
        var otherView = await other.GetFromJsonAsync<JsonElement>($"/api/journal?date={date}", ct);
        var otherToday = await other.GetFromJsonAsync<JsonElement>("/api/journal", ct); // sahibin bugünkü kayıt/giriş olayları da var
        var otherId = (await other.GetFromJsonAsync<JsonElement>("/api/auth/session", ct)).GetProperty("userId").GetGuid();

        first.GetProperty("date").GetString().ShouldBe(date);
        first.GetProperty("items").GetArrayLength().ShouldBe(50);
        first.GetProperty("items")[0].GetProperty("profileName").GetString().ShouldBe("Kanal");
        first.GetProperty("hasMore").GetBoolean().ShouldBeTrue();
        more.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("message").GetString()).ShouldBe(["kayıt 02", "kayıt 01", "gün başı"]);
        more.GetProperty("hasMore").GetBoolean().ShouldBeFalse();
        errors.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("message").GetString()).ShouldBe(["video", "gün başı"]);
        otherView.ToString().ShouldNotContain("kayıt 01");
        var otherItems = otherToday.GetProperty("items").EnumerateArray().ToList();
        otherItems.ShouldNotBeEmpty();
        otherItems.ShouldAllBe(i => i.GetProperty("userId").GetGuid() == otherId);
    }
}
