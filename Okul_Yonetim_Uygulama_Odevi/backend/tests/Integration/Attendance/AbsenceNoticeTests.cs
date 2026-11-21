using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Application.Ports;
using OkulYonetim.Persistence.Repositories;
using OkulYonetim.Tests.Integration.Account;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Attendance;

/// <summary>
/// Spec 0010 AC-7: "Yok" kaydedilen öğrencinin okulda etkin her velisine bir uygulama içi bildirim ve bir e-posta (kuyruğa)
/// gider, metin gereksinim 6'daki gibidir; "Var" ve "Geç" bildirim üretmez; pasifleştirilmiş veliye ve başka öğrencinin
/// velisine ve Veli rolü alınmış (okulda etkin) kişiye gitmez (0010 VERIFY V-3); kayıt işlemi commit edilemezse e-posta kuyruğa
/// girmez, bildirim kalıcı olmaz (V-4, plan Risks 4); SMTP ayarlı değilken (gerçek kuyruk, gönderici "SMTP ayarlı değil") kayıt yine başarılıdır.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AbsenceNoticeTests
{
    private readonly TestDatabase database;

    public AbsenceNoticeTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Absent_NotifiesEachActiveGuardianOnce_PresentAndLateDoNot()
    {
        CapturingOutbox outbox = new();
        using OkulApiFactory factory = Factory(this.database, outbox);
        using HttpClient client = factory.CreateClient();
        AttendanceClass c = await ClassAsync(factory, client);
        Guid ali = await StudentAsync(client, c, "Ali Yılmaz");
        Guid ayse = await StudentAsync(client, c, "Ayşe Kaya");
        Guid berk = await StudentAsync(client, c, "Berk Demir");
        (string mother, _, string motherEmail, _) = await NewGuardianAsync(client, c.Founder, c.School.TenantId, ali);
        (_, Guid fatherId, string fatherEmail, _) = await NewGuardianAsync(client, c.Founder, c.School.TenantId, ali, "Father");
        (string ayseGuardian, _, string ayseEmail, _) = await NewGuardianAsync(client, c.Founder, c.School.TenantId, ayse);
        (string berkGuardian, _, string berkEmail, _) = await NewGuardianAsync(client, c.Founder, c.School.TenantId, berk);
        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/members/{fatherId}/deactivate", c.Founder)).ShouldBe(HttpStatusCode.OK);

        JsonElement result = await SavedAsync(client, c.Teacher, c.ClassroomId, [(ali, "Absent"), (ayse, "Late"), (berk, "Present")]);

        result.GetProperty("notifiedGuardians").GetInt32().ShouldBe(1, "pasif baba hariç");
        string expected = $"Ali Yılmaz bugün (20.11.2026) {c.School.SchoolName}'nda yoklamada gelmedi olarak işaretlendi. Bilginiz yoksa okulla iletişime geçin.";
        EmailMessage email = outbox.To(motherEmail).ShouldHaveSingleItem();
        email.Subject.ShouldBe($"{c.School.SchoolName} — devamsızlık bildirimi");
        email.Body.ShouldStartWith(expected);
        outbox.To(fatherEmail).ShouldBeEmpty();
        outbox.To(ayseEmail).ShouldBeEmpty("Geç bildirim üretmez");
        outbox.To(berkEmail).ShouldBeEmpty("Var bildirim üretmez");
        outbox.Messages.Count.ShouldBe(1);

        JsonElement notice = (await NoticesAsync(client, mother)).Body.EnumerateArray().ShouldHaveSingleItem();
        notice.GetProperty("studentId").GetGuid().ShouldBe(ali);
        notice.GetProperty("studentName").GetString().ShouldBe("Ali Yılmaz");
        notice.GetProperty("kind").GetString().ShouldBe("Absent");
        notice.GetProperty("day").GetString().ShouldBe(Today);
        notice.GetProperty("text").GetString().ShouldBe(expected);
        notice.GetProperty("isRead").GetBoolean().ShouldBeFalse();
        (await NoticesAsync(client, ayseGuardian)).Body.GetArrayLength().ShouldBe(0);
        (await NoticesAsync(client, berkGuardian)).Body.GetArrayLength().ShouldBe(0);
        (await NoticeCountAsync(factory, ali)).ShouldBe(1);
    }

    [Fact]
    public async Task Absent_GuardianWhoseGuardianRoleWasRemoved_GetsNoNotice()
    {
        CapturingOutbox outbox = new();
        using OkulApiFactory factory = Factory(this.database, outbox);
        using HttpClient client = factory.CreateClient();
        AttendanceClass c = await ClassAsync(factory, client);
        Guid ali = await StudentAsync(client, c, "Ali Yılmaz");
        (_, _, string motherEmail, _) = await NewGuardianAsync(client, c.Founder, c.School.TenantId, ali);
        (_, Guid fatherId, string fatherEmail, _) = await NewGuardianAsync(client, c.Founder, c.School.TenantId, ali, "Father");
        var (roleStatus, member, raw) = await CallAsync(client, HttpMethod.Put, $"/api/admin/members/{fatherId}/roles", c.Founder, new { roles = new[] { Teacher(c.School.BranchId) } });
        roleStatus.ShouldBe(HttpStatusCode.OK, raw);
        member.GetProperty("isActive").GetBoolean().ShouldBeTrue("baba okulda etkin (öğretmen), yalnız Veli rolü alındı");

        JsonElement result = await SavedAsync(client, c.Teacher, c.ClassroomId, [(ali, "Absent")]);

        result.GetProperty("notifiedGuardians").GetInt32().ShouldBe(1, "Veli rolü alınan baba hariç (0010 VERIFY V-3)");
        outbox.To(motherEmail).ShouldHaveSingleItem();
        outbox.To(fatherEmail).ShouldBeEmpty();
        (await NoticeCountAsync(factory, ali)).ShouldBe(1, "babaya uygulama içi bildirim de yok");
    }

    [Fact]
    public async Task Absent_SaveTransactionFails_NoEmailQueued_NothingPersisted()
    {
        CapturingOutbox outbox = new();
        using OkulApiFactory factory = Factory(this.database, outbox, configure: services =>
        {
            services.RemoveAll<IUnitOfWork>();
            services.AddScoped<EfUnitOfWork>();
            services.AddScoped<IUnitOfWork>(sp => new FailingCommitUnitOfWork(sp.GetRequiredService<EfUnitOfWork>()));
        });
        using HttpClient client = factory.CreateClient();
        AttendanceClass c = await ClassAsync(factory, client);
        Guid ali = await StudentAsync(client, c, "Ali Yılmaz");
        (string mother, _, _, _) = await NewGuardianAsync(client, c.Founder, c.School.TenantId, ali);

        var (status, _, _) = await SaveAsync(client, c.Teacher, c.ClassroomId, [(ali, "Absent")]);

        status.ShouldBe(HttpStatusCode.InternalServerError, "yoklama işlemi commit edilemedi");
        outbox.Messages.ShouldBeEmpty("geri alınan yoklama e-posta göndermez (0010 VERIFY V-4, plan Risks 4)");
        (await NoticeCountAsync(factory, ali)).ShouldBe(0, "bildirim satırı kalıcı olmadı");
        (await SheetCountAsync(factory, c.ClassroomId)).ShouldBe(0, "yoklama kalıcı olmadı");
        (await NoticesAsync(client, mother)).Body.GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task SmtpNotConfigured_SaveStillSucceeds_NoticeKept()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: new FakeTimeProvider(Morning));
        using HttpClient client = factory.CreateClient();
        AttendanceClass c = await ClassAsync(factory, client);
        Guid ali = await StudentAsync(client, c, "Ali Yılmaz");
        (string mother, _, _, _) = await NewGuardianAsync(client, c.Founder, c.School.TenantId, ali);

        var (status, result, _) = await SaveAsync(client, c.Teacher, c.ClassroomId, [(ali, "Absent")]);

        status.ShouldBe(HttpStatusCode.OK);
        result.GetProperty("notifiedGuardians").GetInt32().ShouldBe(1);
        (await NoticesAsync(client, mother)).Body.GetArrayLength().ShouldBe(1);
    }
}
