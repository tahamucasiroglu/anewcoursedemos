using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Communication;

/// <summary>
/// Spec 0011 AC-8: boş ya da 2000 karakteri aşan mesaj reddedilir (2000 kabul); konuşma listesi son mesaja göre, karşı taraf,
/// öğrenci adı ve okunmamış sayısıyla; konuşma açılınca okunmamış sayısı sıfırlanır; son 50 mesaj ve "Daha eski" ile önceki
/// 50 gelir — aynı anda gönderilmiş mesajlar da sayfa sınırında kaybolmaz (review L-1); konuşma yokken liste boş.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ConversationFlowTests
{
    private readonly TestDatabase database;

    public ConversationFlowTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Send_EmptyOrTooLong_Rejected_2000Accepted()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid id = await StartedAsync(client, w.AliMother.Token, w.Ali, w.Class.TeacherId);

        var (emptyStatus, empty, _) = await SendAsync(client, w.AliMother.Token, id, "   ");
        var (longStatus, tooLong, _) = await SendAsync(client, w.AliMother.Token, id, new string('m', 2001));
        emptyStatus.ShouldBe(HttpStatusCode.BadRequest);
        Messages(empty).ShouldContain("Mesaj boş olamaz");
        longStatus.ShouldBe(HttpStatusCode.BadRequest);
        Messages(tooLong).ShouldContain("Mesaj en fazla 2000 karakter olabilir");
        (await MessagesAsync(client, w.AliMother.Token, id)).Body.GetProperty("messages").GetArrayLength().ShouldBe(0);

        var (okStatus, ok, _) = await SendAsync(client, w.AliMother.Token, id, new string('m', 2000));
        okStatus.ShouldBe(HttpStatusCode.OK);
        ok.GetProperty("message").GetProperty("body").GetString()!.Length.ShouldBe(2000);
        ok.GetProperty("message").GetProperty("mine").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task List_ByLastMessage_WithCounterpartStudentAndUnread_OpeningResetsUnread()
    {
        FakeTimeProvider time = new(Morning);
        using OkulApiFactory factory = Factory(this.database, time: time);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (await ConversationsAsync(client, w.Class.Teacher)).Body.GetArrayLength().ShouldBe(0, "konuşma yokken boş");
        Guid withAli = await StartedAsync(client, w.AliMother.Token, w.Ali, w.Class.TeacherId);
        Guid withAyse = await StartedAsync(client, w.AyseMother.Token, w.Ayse, w.Class.TeacherId);
        time.Advance(TimeSpan.FromMinutes(1));
        await SendAsync(client, w.AyseMother.Token, withAyse, "Ayşe bugün gelmeyecek");
        time.Advance(TimeSpan.FromMinutes(1));
        await SendAsync(client, w.AliMother.Token, withAli, "Ali'nin ödevi");
        time.Advance(TimeSpan.FromMinutes(1));
        await SendAsync(client, w.AliMother.Token, withAli, "Teşekkürler");

        JsonElement[] list = [.. (await ConversationsAsync(client, w.Class.Teacher)).Body.EnumerateArray()];
        list.Select(c => c.GetProperty("id").GetGuid()).ShouldBe([withAli, withAyse]);
        list[0].GetProperty("studentName").GetString().ShouldBe("Ali Yılmaz");
        list[0].GetProperty("counterpartUserId").GetGuid().ShouldBe(w.AliMother.UserId);
        list[0].GetProperty("counterpartName").GetString().ShouldBe("Veli Kişi");
        list[0].GetProperty("unreadCount").GetInt32().ShouldBe(2);
        list[1].GetProperty("unreadCount").GetInt32().ShouldBe(1);
        (await MeAsync(client, w.Class.Teacher)).GetProperty("unreadMessageCount").GetInt32().ShouldBe(3);
        (await ConversationsAsync(client, w.AliMother.Token)).Body[0].GetProperty("unreadCount").GetInt32().ShouldBe(0, "kendi mesajları okunmamış sayılmaz");

        JsonElement thread = (await MessagesAsync(client, w.Class.Teacher, withAli)).Body;
        thread.GetProperty("conversation").GetProperty("unreadCount").GetInt32().ShouldBe(0);
        thread.GetProperty("messages").EnumerateArray().Select(m => m.GetProperty("body").GetString()).ShouldBe(["Ali'nin ödevi", "Teşekkürler"]);
        thread.GetProperty("messages")[0].GetProperty("mine").GetBoolean().ShouldBeFalse();
        (await ConversationsAsync(client, w.Class.Teacher)).Body[0].GetProperty("unreadCount").GetInt32().ShouldBe(0);
        (await MeAsync(client, w.Class.Teacher)).GetProperty("unreadMessageCount").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task Messages_Last50_ThenOlderPage()
    {
        FakeTimeProvider time = new(Morning);
        using OkulApiFactory factory = Factory(this.database, time: time);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid id = await StartedAsync(client, w.AliMother.Token, w.Ali, w.Class.TeacherId);
        for (int i = 1; i <= 55; i++)
        {
            time.Advance(TimeSpan.FromSeconds(1));
            (await SendAsync(client, i % 2 == 0 ? w.Class.Teacher : w.AliMother.Token, id, $"Mesaj {i}")).Status.ShouldBe(HttpStatusCode.OK);
        }

        JsonElement latest = (await MessagesAsync(client, w.AliMother.Token, id)).Body;
        string[] latestBodies = [.. latest.GetProperty("messages").EnumerateArray().Select(m => m.GetProperty("body").GetString()!)];
        latestBodies.Length.ShouldBe(50);
        latestBodies[0].ShouldBe("Mesaj 6");
        latestBodies[49].ShouldBe("Mesaj 55");
        latest.GetProperty("hasMore").GetBoolean().ShouldBeTrue();

        Guid oldest = latest.GetProperty("messages")[0].GetProperty("id").GetGuid();
        JsonElement older = (await MessagesAsync(client, w.AliMother.Token, id, oldest)).Body;
        older.GetProperty("messages").EnumerateArray().Select(m => m.GetProperty("body").GetString()).ShouldBe(["Mesaj 1", "Mesaj 2", "Mesaj 3", "Mesaj 4", "Mesaj 5"]);
        older.GetProperty("hasMore").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task Messages_51SentAtSameInstant_OlderPageReturnsTheRest_NoneLostOrRepeated()
    {
        using OkulApiFactory factory = Factory(this.database, time: new FakeTimeProvider(Morning));
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid id = await StartedAsync(client, w.AliMother.Token, w.Ali, w.Class.TeacherId);
        for (int i = 1; i <= 51; i++)
        {
            (await SendAsync(client, w.AliMother.Token, id, $"Mesaj {i}")).Status.ShouldBe(HttpStatusCode.OK);
        }

        JsonElement latest = (await MessagesAsync(client, w.AliMother.Token, id)).Body;
        latest.GetProperty("messages").GetArrayLength().ShouldBe(50);
        latest.GetProperty("hasMore").GetBoolean().ShouldBeTrue();
        JsonElement older = (await MessagesAsync(client, w.AliMother.Token, id, latest.GetProperty("messages")[0].GetProperty("id").GetGuid())).Body;
        older.GetProperty("messages").GetArrayLength().ShouldBe(1, "aynı anda gönderilen 51. mesaj 'Daha eski' sayfasında");
        older.GetProperty("hasMore").GetBoolean().ShouldBeFalse();

        string[] all = [.. older.GetProperty("messages").EnumerateArray().Concat(latest.GetProperty("messages").EnumerateArray()).Select(m => m.GetProperty("body").GetString()!)];
        all.Order(StringComparer.Ordinal).ShouldBe(Enumerable.Range(1, 51).Select(i => $"Mesaj {i}").Order(StringComparer.Ordinal));
    }
}
