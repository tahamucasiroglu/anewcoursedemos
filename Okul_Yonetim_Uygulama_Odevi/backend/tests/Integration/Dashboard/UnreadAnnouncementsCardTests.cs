using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Tests.Integration.Billing;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Billing.BillingTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;
using static OkulYonetim.Tests.Integration.Dashboard.DashboardTestData;

namespace OkulYonetim.Tests.Integration.Dashboard;

/// <summary>
/// Spec 0017 AC-5 (okunmayan duyurular kartı): son 7 günde (bugün dahil — 14 Kasım 00:00 Türkiye dahil, 13 Kasım 23:59 Türkiye değil)
/// yayınlanan ve okumayanı olan duyurular "X / Y okudu" sayılarıyla, en çok okunmayandan (eşitlikte yeni önce) listelenir; herkesin okuduğu
/// duyuru listede yok; sayılar 0011'in sonuç ekranıyla aynı; okumayan veli sayısı farklı veli sayısıdır; en fazla 10 satır; duyuru yoksa 0.
/// </summary>
[Trait("Category", "Integration")]
public sealed class UnreadAnnouncementsCardTests
{
    private readonly TestDatabase database;

    public UnreadAnnouncementsCardTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task LastSevenDays_UnreadOnly_OrderedAndCountsMatchResults()
    {
        FakeTimeProvider clock = Clock(new DateTimeOffset(2026, 11, 13, 9, 0, 0, TimeSpan.Zero));
        using OkulApiFactory factory = DashboardFactory(this.database, clock);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await DashboardWorldAsync(factory, client);
        CommunicationWorld s = w.School;

        clock.SetUtcNow(new DateTimeOffset(2026, 11, 13, 20, 59, 0, TimeSpan.Zero));
        string founder = await FounderTokenAsync(client, w);
        await PublishedAsync(client, founder, Plain("School", null, title: "Eski duyuru"));
        clock.SetUtcNow(new DateTimeOffset(2026, 11, 13, 21, 0, 0, TimeSpan.Zero));
        Guid edge = await PublishedAsync(client, founder, Plain("School", null, title: "Sınırdaki duyuru"));

        clock.SetUtcNow(Attendance.AttendanceTestData.Morning);
        founder = await FounderTokenAsync(client, w);
        Guid allRead = await PublishedAsync(client, founder, Plain("Classroom", s.OtherClassroomId, title: "Herkes okudu"));
        clock.Advance(TimeSpan.FromMinutes(1));
        Guid meeting = await PublishedAsync(client, founder, Plain("Branch", w.BranchId, title: "Veli toplantısı"));
        clock.Advance(TimeSpan.FromMinutes(1));
        Guid aboutAli = await PublishedAsync(client, founder, Plain("Student", w.Ali, title: "Ali hakkında"));

        (await DetailAsync(client, await ReloginAsync(client, s.Class, s.BerkMother), allRead)).Status.ShouldBe(HttpStatusCode.OK);
        (await DetailAsync(client, await ReloginAsync(client, s.Class, s.AliMother), meeting)).Status.ShouldBe(HttpStatusCode.OK);
        (await DetailAsync(client, await ReloginAsync(client, s.Class, s.AyseMother), meeting)).Status.ShouldBe(HttpStatusCode.OK);
        (await DetailAsync(client, await ReloginAsync(client, s.Class, s.AliFather), edge)).Status.ShouldBe(HttpStatusCode.OK);

        var (status, card, raw) = await AnnouncementsCardAsync(client, founder);

        status.ShouldBe(HttpStatusCode.OK, raw);
        card.GetProperty("since").GetString().ShouldBe("2026-11-14");
        card.GetProperty("announcementCount").GetInt32().ShouldBe(3);
        JsonElement top = card.GetProperty("top");
        Names(top, "title").ShouldBe(["Sınırdaki duyuru", "Ali hakkında", "Veli toplantısı"]);
        card.GetProperty("unreadGuardianCount").GetInt32().ShouldBe(5, "Ali'nin annesi ve babası, Ayşe, Berk ve Cem'in anneleri");
        raw.ShouldNotContain("Eski duyuru");
        raw.ShouldNotContain("Herkes okudu");
        raw.ShouldNotContain("Cuma 14:00");
        foreach (JsonElement row in top.EnumerateArray())
        {
            JsonElement results = (await ResultsAsync(client, founder, row.GetProperty("id").GetGuid())).Body;
            row.GetProperty("recipientCount").GetInt32().ShouldBe(results.GetProperty("recipientCount").GetInt32());
            row.GetProperty("readCount").GetInt32().ShouldBe(results.GetProperty("readCount").GetInt32());
        }

        top[0].GetProperty("recipientCount").GetInt32().ShouldBe(5);
        top[0].GetProperty("readCount").GetInt32().ShouldBe(1);
        top[0].GetProperty("authorName").GetString().ShouldBe("Ayşe Kurucu");
        top[2].GetProperty("readCount").GetInt32().ShouldBe(2);
        top[2].GetProperty("recipientCount").GetInt32().ShouldBe(4);
        top[1].GetProperty("id").GetGuid().ShouldBe(aboutAli);
    }

    [Fact]
    public async Task MoreThanTen_ShowsTen_NoneShowsZero()
    {
        using OkulApiFactory factory = DashboardFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await DashboardWorldAsync(factory, client);
        JsonElement empty = (await AnnouncementsCardAsync(client, w.Founder)).Body;
        empty.GetProperty("announcementCount").GetInt32().ShouldBe(0);
        empty.GetProperty("unreadGuardianCount").GetInt32().ShouldBe(0);
        empty.GetProperty("top").GetArrayLength().ShouldBe(0);

        for (int i = 0; i < 11; i++)
        {
            await PublishedAsync(client, w.Founder, Plain("School", null, title: $"Duyuru {i:00}"));
        }

        JsonElement card = (await AnnouncementsCardAsync(client, w.Founder)).Body;
        card.GetProperty("announcementCount").GetInt32().ShouldBe(11);
        card.GetProperty("top").GetArrayLength().ShouldBe(10);
    }
}
