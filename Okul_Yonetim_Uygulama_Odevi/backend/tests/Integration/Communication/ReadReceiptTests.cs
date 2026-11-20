using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Communication;

/// <summary>
/// Spec 0011 AC-4: veli ayrıntıyı açınca okundu kaydedilir, ikinci açış ilk okuma zamanını değiştirmez; yayınlayan ve kurucu
/// "X / Y veli okudu" ile okumayanları (veli ad-soyad + çocuk adı) görür; yayınlayan olmayan öğretmen 404, veli 403 alır.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ReadReceiptTests
{
    private readonly TestDatabase database;

    public ReadReceiptTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Open_FirstTimeMarksRead_SecondOpenKeepsFirstTime()
    {
        FakeTimeProvider time = new(Morning);
        using OkulApiFactory factory = Factory(this.database, time: time);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid id = await PublishedAsync(client, w.Class.Teacher, Plain("Classroom", w.Class.ClassroomId));
        time.Advance(TimeSpan.FromMinutes(5));
        DateTimeOffset firstOpen = time.GetUtcNow();

        (await DetailAsync(client, w.AliMother.Token, id)).Status.ShouldBe(HttpStatusCode.OK);
        time.Advance(TimeSpan.FromMinutes(5));
        (await DetailAsync(client, w.AliMother.Token, id)).Status.ShouldBe(HttpStatusCode.OK);

        AnnouncementRecipient recipient = await RecipientAsync(factory, id, w.AliMother.UserId);
        recipient.ReadAt.ShouldBe(firstOpen);
        (await RecipientAsync(factory, id, w.AliFather.UserId)).ReadAt.ShouldBeNull();
    }

    [Fact]
    public async Task Results_AuthorAndFounder_ReadCountAndUnreadList_OthersRejected()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid id = await PublishedAsync(client, w.Class.Teacher, Plain("Classroom", w.Class.ClassroomId));
        await DetailAsync(client, w.AliMother.Token, id);

        foreach (string viewer in new[] { w.Class.Teacher, w.Class.Founder })
        {
            var (status, results, _) = await ResultsAsync(client, viewer, id);
            status.ShouldBe(HttpStatusCode.OK);
            results.GetProperty("recipientCount").GetInt32().ShouldBe(3);
            results.GetProperty("readCount").GetInt32().ShouldBe(1);
            JsonElement[] unread = [.. results.GetProperty("unread").EnumerateArray()];
            unread.Length.ShouldBe(2);
            unread.ShouldAllBe(u => u.GetProperty("guardianName").GetString() == "Veli Kişi");
            unread.Select(u => u.GetProperty("childNames")[0].GetString()).ShouldBe(["Ali Yılmaz", "Ayşe Kaya"], ignoreOrder: true);
            results.GetProperty("consent").ValueKind.ShouldBe(JsonValueKind.Null);
        }

        (await ResultsAsync(client, w.OtherTeacher, id)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await DetailAsync(client, w.OtherTeacher, id)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await ResultsAsync(client, w.AliMother.Token, id)).Status.ShouldBe(HttpStatusCode.Forbidden);
    }
}
