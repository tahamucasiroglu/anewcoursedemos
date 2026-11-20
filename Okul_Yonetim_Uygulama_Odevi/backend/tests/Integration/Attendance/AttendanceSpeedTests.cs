using System.Diagnostics;
using System.Net;
using OkulYonetim.Tests.Integration.Account;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Attendance;

/// <summary>
/// Spec 0010 AC-5 (plan Risks 8): 40 öğrencilik sınıfta (her öğrencinin bir velisi var) liste açılması ve 3 devamsızlı
/// kaydetme yerel makinede her biri ≤ 1 sn — ilk (ısınma) ölçüm hariç 5 ölçümün ortancası. Her kaydetme farklı üç öğrenciyi
/// "Yok" yapar: bildirim ve düzeltme yolu da ölçülür. Ağ gecikmesi hariç (test sunucusu bellekte).
/// </summary>
[Trait("Category", "Integration")]
public sealed class AttendanceSpeedTests
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(1);
    private readonly TestDatabase database;

    public AttendanceSpeedTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task FortyStudents_ListAndSave_EachUnderOneSecond()
    {
        using OkulApiFactory factory = Factory(this.database, new CapturingOutbox());
        using HttpClient client = factory.CreateClient();
        AttendanceClass c = await ClassAsync(factory, client);
        List<Guid> students = [];
        for (int i = 1; i <= 40; i++)
        {
            Guid id = await StudentAsync(client, c, $"Öğrenci {i:00}");
            await LinkAsync(client, c.Founder, id, Guardian(Identity.IdentityTestData.UniqueEmail("veli")));
            students.Add(id);
        }

        (await TodayAsync(client, c.Teacher, c.ClassroomId)).Status.ShouldBe(HttpStatusCode.OK, "ısınma");
        (await SaveAsync(client, c.Teacher, c.ClassroomId, Marks(students))).Status.ShouldBe(HttpStatusCode.OK, "ısınma");

        List<TimeSpan> lists = [];
        List<TimeSpan> saves = [];
        for (int run = 0; run < 5; run++)
        {
            Stopwatch watch = Stopwatch.StartNew();
            var (listStatus, view, _) = await TodayAsync(client, c.Teacher, c.ClassroomId);
            lists.Add(watch.Elapsed);
            listStatus.ShouldBe(HttpStatusCode.OK);
            view.GetProperty("students").GetArrayLength().ShouldBe(40);

            (Guid, string)[] absent = [.. students.Skip(run * 3).Take(3).Select(id => (id, "Absent"))];
            watch.Restart();
            var (saveStatus, result, _) = await SaveAsync(client, c.Teacher, c.ClassroomId, Marks(students, absent));
            saves.Add(watch.Elapsed);
            saveStatus.ShouldBe(HttpStatusCode.OK);
            result.GetProperty("absent").GetInt32().ShouldBe(3);
        }

        Median(lists).ShouldBeLessThanOrEqualTo(Limit, $"liste ölçümleri: {string.Join(", ", lists.Select(t => (int)t.TotalMilliseconds))} ms");
        Median(saves).ShouldBeLessThanOrEqualTo(Limit, $"kaydetme ölçümleri: {string.Join(", ", saves.Select(t => (int)t.TotalMilliseconds))} ms");
    }

    private static TimeSpan Median(List<TimeSpan> values) => values.Order().ElementAt(values.Count / 2);
}
