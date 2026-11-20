using System.Net;
using System.Text.Json;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Communication;

/// <summary>
/// Spec 0011 AC-7: veli çocuğunun sınıfının öğretmenine, öğretmen kendi sınıfındaki öğrencinin velisine o öğrenci bağlamında
/// yazar; aynı üçlü için ikinci konuşma açılmaz (var olan döner); ilişkisiz öğretmen–veli–öğrenci ve veli–veli 404,
/// kurucu/muhasebe 403; konuşmayı üçüncü kişi (kurucu dahil) okuyamaz (404); ilişki bitince eski mesajlar okunur, yeni mesaj
/// "Bu konuşmaya artık mesaj gönderilemez." ile reddedilir.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ConversationAuthorizationTests
{
    private readonly TestDatabase database;

    public ConversationAuthorizationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Start_BothDirections_SameTripleReturnsSameConversation()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);

        var (status, fromGuardian, _) = await StartAsync(client, w.AliMother.Token, w.Ali, w.Class.TeacherId);
        status.ShouldBe(HttpStatusCode.OK);
        fromGuardian.GetProperty("myRole").GetString().ShouldBe("Guardian");
        fromGuardian.GetProperty("studentName").GetString().ShouldBe("Ali Yılmaz");
        Guid id = fromGuardian.GetProperty("id").GetGuid();

        (await StartedAsync(client, w.AliMother.Token, w.Ali, w.Class.TeacherId)).ShouldBe(id);
        JsonElement fromTeacher = (await StartAsync(client, w.Class.Teacher, w.Ali, w.AliMother.UserId)).Body;
        fromTeacher.GetProperty("id").GetGuid().ShouldBe(id);
        fromTeacher.GetProperty("myRole").GetString().ShouldBe("Teacher");
        fromTeacher.GetProperty("counterpartName").GetString().ShouldBe("Veli Kişi");

        (await StartedAsync(client, w.AliFather.Token, w.Ali, w.Class.TeacherId)).ShouldNotBe(id, "başka veli = başka üçlü");
    }

    [Fact]
    public async Task Start_Unrelated_Rejected()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        string accountantEmail = UniqueEmail("muhasebe");
        await AddMemberAsync(factory, accountantEmail, w.Class.School.TenantId, Role.Accounting, w.Class.School.BranchId);
        string accountant = await TokenForSchoolAsync(client, accountantEmail, w.Class.School.TenantId);

        var (otherTeacher, body, _) = await StartAsync(client, w.AliMother.Token, w.Ali, w.OtherTeacherId);
        otherTeacher.ShouldBe(HttpStatusCode.NotFound, "Ali'nin sınıfının öğretmeni değil");
        Detail(body).ShouldBe("Bu kişiyle bu öğrenci için mesajlaşılamaz");
        (await StartAsync(client, w.AliMother.Token, w.Ali, w.AyseMother.UserId)).Status.ShouldBe(HttpStatusCode.NotFound, "veli–veli");
        (await StartAsync(client, w.AliMother.Token, w.Ayse, w.Class.TeacherId)).Status.ShouldBe(HttpStatusCode.NotFound, "kendi çocuğu değil");
        (await StartAsync(client, w.OtherTeacher, w.Ali, w.AliMother.UserId)).Status.ShouldBe(HttpStatusCode.NotFound, "kendi sınıfı değil");
        (await StartAsync(client, w.Class.Teacher, w.Ali, w.AyseMother.UserId)).Status.ShouldBe(HttpStatusCode.NotFound, "Ali'nin velisi değil");
        (await StartAsync(client, w.Class.Founder, w.Ali, w.AliMother.UserId)).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await StartAsync(client, accountant, w.Ali, w.AliMother.UserId)).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await ConversationsAsync(client, w.AliMother.Token)).Body.GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task ThirdParty_CannotReadOrWrite_NotFound()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid id = await StartedAsync(client, w.AliMother.Token, w.Ali, w.Class.TeacherId);
        (await SendAsync(client, w.AliMother.Token, id, "Merhaba hocam")).Status.ShouldBe(HttpStatusCode.OK);

        foreach (string outsider in new[] { w.Class.Founder, w.OtherTeacher, w.AliFather.Token, w.AyseMother.Token })
        {
            (await MessagesAsync(client, outsider, id)).Status.ShouldBe(HttpStatusCode.NotFound);
            (await SendAsync(client, outsider, id, "araya giriyorum")).Status.ShouldBe(HttpStatusCode.NotFound);
        }

        (await ConversationsAsync(client, w.Class.Founder)).Body.GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task RelationEnded_OldMessagesReadable_NewMessageRejected()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid id = await StartedAsync(client, w.AliMother.Token, w.Ali, w.Class.TeacherId);
        (await SendAsync(client, w.AliMother.Token, id, "Ödev ne?")).Status.ShouldBe(HttpStatusCode.OK);

        await AssignAsync(client, w.Class.Founder, w.Class.ClassroomId);

        foreach (string party in new[] { w.AliMother.Token, w.Class.Teacher })
        {
            var (status, thread, _) = await MessagesAsync(client, party, id);
            status.ShouldBe(HttpStatusCode.OK);
            thread.GetProperty("canSend").GetBoolean().ShouldBeFalse();
            thread.GetProperty("messages").EnumerateArray().ShouldHaveSingleItem().GetProperty("body").GetString().ShouldBe("Ödev ne?");
            var (sendStatus, problem, _) = await SendAsync(client, party, id, "Yeni mesaj");
            sendStatus.ShouldBe(HttpStatusCode.BadRequest);
            Detail(problem).ShouldBe("Bu konuşmaya artık mesaj gönderilemez.");
        }

        (await StartAsync(client, w.AliMother.Token, w.Ali, w.Class.TeacherId)).Status.ShouldBe(HttpStatusCode.NotFound);
    }
}
