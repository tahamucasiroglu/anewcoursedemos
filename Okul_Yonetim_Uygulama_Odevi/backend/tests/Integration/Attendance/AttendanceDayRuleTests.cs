using System.Net;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Tests.Integration.Account;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;

namespace OkulYonetim.Tests.Integration.Attendance;

/// <summary>
/// Spec 0010 AC-6: yalnızca bugün (Türkiye günü) kaydedilir ve aynı gün düzeltilir; Türkiye 23:59'da gün hâlâ bugündür,
/// 00:00 olunca dünkü yoklama kilitlenir ("Geçmiş günün yoklaması değiştirilemez"); başka bir gün için yoklama gönderilemez;
/// dönem dışında ve takvimde tatil olan günde liste "okul günü değil" döner ve kayıt reddedilir ("Bugün okul günü değil").
/// </summary>
[Trait("Category", "Integration")]
public sealed class AttendanceDayRuleTests
{
    private readonly TestDatabase database;

    public AttendanceDayRuleTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task TurkeyMidnight_LocksYesterday_FutureDayRejected()
    {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 20, 20, 59, 0, TimeSpan.Zero));
        using OkulApiFactory factory = Factory(this.database, new CapturingOutbox(), time);
        using HttpClient client = factory.CreateClient();
        AttendanceClass c = await ClassAsync(factory, client);
        Guid ali = await StudentAsync(client, c, "Ali Yılmaz");

        (await TodayAsync(client, c.Teacher, c.ClassroomId)).Body.GetProperty("day").GetString().ShouldBe("2026-11-20", "Türkiye 23:59");
        (await SaveAsync(client, c.Teacher, c.ClassroomId, [(ali, "Absent")], "2026-11-20")).Status.ShouldBe(HttpStatusCode.OK);
        (await SaveAsync(client, c.Teacher, c.ClassroomId, [(ali, "Present")], "2026-11-20")).Status.ShouldBe(HttpStatusCode.OK, "aynı gün düzeltme");
        var (future, futureBody, _) = await SaveAsync(client, c.Teacher, c.ClassroomId, [(ali, "Present")], "2026-11-21");
        future.ShouldBe(HttpStatusCode.BadRequest);
        Detail(futureBody).ShouldBe("Yalnızca bugünün yoklaması alınabilir");

        time.Advance(TimeSpan.FromMinutes(1));

        var (_, view, _) = await TodayAsync(client, c.Teacher, c.ClassroomId);
        view.GetProperty("day").GetString().ShouldBe("2026-11-21", "Türkiye 00:00");
        view.GetProperty("isSaved").GetBoolean().ShouldBeFalse("yeni gün");
        var (past, pastBody, _) = await SaveAsync(client, c.Teacher, c.ClassroomId, [(ali, "Absent")], "2026-11-20");
        past.ShouldBe(HttpStatusCode.BadRequest);
        Detail(pastBody).ShouldBe("Geçmiş günün yoklaması değiştirilemez");
        (await SaveAsync(client, c.Teacher, c.ClassroomId, [(ali, "Absent")], "2026-11-21")).Status.ShouldBe(HttpStatusCode.OK, "cumartesi engellenmez");
        (await SheetCountAsync(factory, c.ClassroomId)).ShouldBe(2);
    }

    [Fact]
    public async Task HolidayInCalendar_NotSchoolDay_SaveRejected()
    {
        using OkulApiFactory factory = Factory(this.database, new CapturingOutbox());
        using HttpClient client = factory.CreateClient();
        AttendanceClass c = await ClassAsync(factory, client);
        Guid ali = await StudentAsync(client, c, "Ali Yılmaz");
        await CreateCalendarEntryAsync(client, c.Founder, c.TermId, "Sınav haftası", "Exam", "2026-11-16", "2026-11-20");
        Guid holiday = await CreateCalendarEntryAsync(client, c.Founder, c.TermId, "Ara tatil", "Holiday", "2026-11-19", "2026-11-20");

        var (status, view, _) = await TodayAsync(client, c.Teacher, c.ClassroomId);
        status.ShouldBe(HttpStatusCode.OK);
        view.GetProperty("isSchoolDay").GetBoolean().ShouldBeFalse();
        view.GetProperty("students").GetArrayLength().ShouldBe(0);
        var (rejected, body, _) = await SaveAsync(client, c.Teacher, c.ClassroomId, [(ali, "Absent")]);
        rejected.ShouldBe(HttpStatusCode.BadRequest);
        Detail(body).ShouldBe("Bugün okul günü değil");
        (await SheetCountAsync(factory, c.ClassroomId)).ShouldBe(0);

        (await StatusAsync(client, HttpMethod.Delete, $"/api/admin/calendar-entries/{holiday}", c.Founder)).ShouldBe(HttpStatusCode.NoContent);
        (await TodayAsync(client, c.Teacher, c.ClassroomId)).Body.GetProperty("isSchoolDay").GetBoolean().ShouldBeTrue("silinen tatil; sınav günü okul günüdür");
        (await SaveAsync(client, c.Teacher, c.ClassroomId, [(ali, "Absent")])).Status.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OutsideTerm_NotSchoolDay_SaveRejected()
    {
        using OkulApiFactory factory = Factory(this.database, new CapturingOutbox());
        using HttpClient client = factory.CreateClient();
        AttendanceClass c = await ClassAsync(factory, client, termStart: "2026-12-01", termEnd: "2027-01-22");
        Guid ali = await StudentAsync(client, c, "Ali Yılmaz");

        var (_, view, _) = await TodayAsync(client, c.Teacher, c.ClassroomId);
        view.GetProperty("isSchoolDay").GetBoolean().ShouldBeFalse("dönem henüz başlamadı");
        var (rejected, body, _) = await SaveAsync(client, c.Teacher, c.ClassroomId, [(ali, "Absent")]);
        rejected.ShouldBe(HttpStatusCode.BadRequest);
        Detail(body).ShouldBe("Bugün okul günü değil");
    }
}
