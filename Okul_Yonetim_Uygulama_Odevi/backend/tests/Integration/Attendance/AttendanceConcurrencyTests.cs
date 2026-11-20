using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OkulYonetim.Application.Ports;
using OkulYonetim.Persistence.Repositories;
using OkulYonetim.Tests.Integration.Account;
using OkulYonetim.Tests.Integration.Administration;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Attendance;

/// <summary>
/// Spec 0010 AC-11 (plan Risks 5): aynı sınıf ve gün için aynı anda gönderilen iki kayıt (iki öğretmen) — ilk istek sıra
/// içindeki "yoklama var mı" okumasında ikinci istek gönderildikten sonra en fazla 2 sn bekletilir (<see cref="RaceGate"/>).
/// Sınıf + gün sırası varken ikinci istek bu okumaya giremez; ilki bitince yoklamayı görür ve düzeltme olarak kaydeder.
/// Sonuç: tek yoklama; her veli aynı öğrenci için tek "yok" bildirimi ve tek e-posta alır.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AttendanceConcurrencyTests
{
    private readonly TestDatabase database;

    public AttendanceConcurrencyTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task TwoSavesAtOnce_OneSheet_OneNoticeAndEmailPerGuardian()
    {
        RaceGate gate = new();
        CapturingOutbox outbox = new();
        using OkulApiFactory factory = Factory(this.database, outbox, configure: services =>
        {
            services.RemoveAll<IAttendanceRepository>();
            services.AddScoped<AttendanceRepository>();
            services.AddScoped<IAttendanceRepository>(sp => new GatedAttendanceRepository(sp.GetRequiredService<AttendanceRepository>(), gate));
        });
        using HttpClient client = factory.CreateClient();
        AttendanceClass c = await ClassAsync(factory, client);
        (Guid secondId, _, string second) = await TeacherAsync(factory, client, c.School.TenantId, c.School.BranchId);
        await AssignAsync(client, c.Founder, c.ClassroomId, c.TeacherId, secondId);
        Guid ali = await StudentAsync(client, c, "Ali Yılmaz");
        Guid ayse = await StudentAsync(client, c, "Ayşe Kaya");
        (string mother, _, string motherEmail, _) = await NewGuardianAsync(client, c.Founder, c.School.TenantId, ali);
        (_, _, string ayseEmail, _) = await NewGuardianAsync(client, c.Founder, c.School.TenantId, ayse);

        gate.Enabled = true;
        var first = SaveAsync(client, c.Teacher, c.ClassroomId, [(ali, "Absent"), (ayse, "Absent")]);
        await gate.FirstEntered.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var secondSave = SaveAsync(client, second, c.ClassroomId, [(ali, "Absent"), (ayse, "Absent")]);
        var results = await Task.WhenAll(first, secondSave);
        gate.Enabled = false;

        gate.SecondEnteredWhileFirstWaited.ShouldBe(false, "ikinci istek sırayı beklemeden yoklamayı okudu");
        results.ShouldAllBe(r => r.Status == HttpStatusCode.OK);
        results.Sum(r => r.Body.GetProperty("notifiedGuardians").GetInt32()).ShouldBe(2, "her veliye bir kez");
        (await SheetCountAsync(factory, c.ClassroomId)).ShouldBe(1);
        (await NoticeCountAsync(factory, ali)).ShouldBe(1);
        (await NoticeCountAsync(factory, ayse)).ShouldBe(1);
        outbox.To(motherEmail).Length.ShouldBe(1);
        outbox.To(ayseEmail).Length.ShouldBe(1);
        (await NoticesAsync(client, mother)).Body.GetArrayLength().ShouldBe(1);
    }
}
