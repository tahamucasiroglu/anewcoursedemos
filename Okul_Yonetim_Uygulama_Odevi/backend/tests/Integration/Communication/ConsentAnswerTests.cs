using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Communication;

/// <summary>
/// Spec 0011 AC-6: veli kapsamdaki çocuğu için Onaylıyorum/Onaylamıyorum verir ve son güne kadar değiştirir (son cevap
/// geçerli; kim ve ne zaman kayıtlı); öğrencinin öteki velisi aynı cevabı görür ve değiştirebilir; son gün 23:59'dan (Türkiye
/// saati) sonra "Onay süresi doldu."; kapsam dışındaki ya da kendi çocuğu olmayan öğrenci 404; onay formu olmayan duyuruya
/// cevap reddedilir.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ConsentAnswerTests
{
    private readonly TestDatabase database;

    public ConsentAnswerTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Answer_ChangeableUntilDeadline_LastWins_OtherGuardianSeesAndChanges()
    {
        FakeTimeProvider time = new(Morning);
        using OkulApiFactory factory = Factory(this.database, time: time);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid id = await PublishedAsync(client, w.Class.Teacher, Form("Classroom", w.Class.ClassroomId, "2026-11-21"));

        var (status, given, _) = await AnswerAsync(client, w.AliMother.Token, id, w.Ali, "Approved");
        status.ShouldBe(HttpStatusCode.OK);
        given.GetProperty("answer").GetString().ShouldBe("Approved");

        JsonElement fatherView = (await DetailAsync(client, w.AliFather.Token, id)).Body;
        fatherView.GetProperty("consentOpen").GetBoolean().ShouldBeTrue();
        fatherView.GetProperty("consentQuestion").GetString().ShouldBe("Geziye katılsın mı?");
        JsonElement child = fatherView.GetProperty("children").EnumerateArray().ShouldHaveSingleItem();
        child.GetProperty("studentId").GetGuid().ShouldBe(w.Ali);
        child.GetProperty("answer").GetString().ShouldBe("Approved");
        child.GetProperty("answeredAt").GetDateTimeOffset().ShouldBe(Morning);

        time.SetUtcNow(new DateTimeOffset(2026, 11, 21, 20, 59, 0, TimeSpan.Zero));
        string father = await ReloginAsync(client, w.Class, w.AliFather);
        string mother = await ReloginAsync(client, w.Class, w.AliMother);
        var (changedStatus, changed, _) = await AnswerAsync(client, father, id, w.Ali, "Declined");
        changedStatus.ShouldBe(HttpStatusCode.OK, "son gün 23:59 (Türkiye) kabul");
        changed.GetProperty("answer").GetString().ShouldBe("Declined");
        changed.GetProperty("answeredAt").GetDateTimeOffset().ShouldBe(time.GetUtcNow());

        JsonElement motherChild = (await DetailAsync(client, mother, id)).Body.GetProperty("children")[0];
        motherChild.GetProperty("answer").GetString().ShouldBe("Declined");

        time.SetUtcNow(new DateTimeOffset(2026, 11, 21, 21, 0, 0, TimeSpan.Zero));
        var (lateStatus, late, _) = await AnswerAsync(client, mother, id, w.Ali, "Approved");
        lateStatus.ShouldBe(HttpStatusCode.BadRequest);
        Detail(late).ShouldBe("Onay süresi doldu.");
        (await DetailAsync(client, mother, id)).Body.GetProperty("consentOpen").GetBoolean().ShouldBeFalse();
        string teacher = await TokenForSchoolAsync(client, w.Class.TeacherEmail, w.Class.School.TenantId);
        (await ResultsAsync(client, teacher, id)).Body.GetProperty("consent").GetProperty("declined").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task Answer_StudentOutOfScopeOrNotMine_NotFound_PlainAnnouncementRejected()
    {
        using OkulApiFactory factory = CommunicationTestData.Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await LinkAsync(client, w.Class.Founder, w.Berk, Guardian(w.AliMother.Email));
        Guid form = await PublishedAsync(client, w.Class.Founder, Form("Classroom", w.Class.ClassroomId, "2026-11-27"));
        Guid plain = await PublishedAsync(client, w.Class.Founder, Plain("Classroom", w.Class.ClassroomId));

        (await AnswerAsync(client, w.AliMother.Token, form, w.Berk, "Approved")).Status.ShouldBe(HttpStatusCode.NotFound, "kendi çocuğu ama kapsam dışında");
        (await AnswerAsync(client, w.AliMother.Token, form, w.Ayse, "Approved")).Status.ShouldBe(HttpStatusCode.NotFound, "kapsamda ama kendi çocuğu değil");
        (await AnswerAsync(client, w.BerkMother.Token, form, w.Berk, "Approved")).Status.ShouldBe(HttpStatusCode.NotFound, "alıcı değil");
        (await AnswerAsync(client, w.AliMother.Token, form, w.Ali, "Maybe")).Status.ShouldBe(HttpStatusCode.BadRequest);

        var (plainStatus, plainBody, _) = await AnswerAsync(client, w.AliMother.Token, plain, w.Ali, "Approved");
        plainStatus.ShouldBe(HttpStatusCode.BadRequest);
        Detail(plainBody).ShouldBe("Bu duyuru onay istemiyor.");

        (await ResultsAsync(client, w.Class.Founder, form)).Body.GetProperty("consent").GetProperty("pending").GetInt32().ShouldBe(2);
    }
}
