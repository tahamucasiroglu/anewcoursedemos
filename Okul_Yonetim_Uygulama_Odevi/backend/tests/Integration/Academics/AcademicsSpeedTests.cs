using System.Diagnostics;
using System.Net;
using OkulYonetim.Tests.Integration.Attendance;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Academics.AcademicsTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Academics;

/// <summary>
/// Spec 0013 AC-12 (plan Risks 8): 40 öğrencilik sınıfta teslim listesi ve puan listesinin açılması ve kaydı yerel makinede her
/// biri ≤ 1 sn — ilk (ısınma) ölçüm hariç 5 ölçümün ortancası. Her kayıt farklı öğrencileri değiştirir (gerçek yazma ölçülür).
/// Ağ gecikmesi hariç (test sunucusu bellekte). Dokunuş sayısı mobil bileşen testinde.
/// </summary>
[Trait("Category", "Integration")]
[Collection(SpeedMeasurementGroup.Name)]
public sealed class AcademicsSpeedTests
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(1);
    private readonly TestDatabase database;

    public AcademicsSpeedTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task FortyStudents_SubmissionAndGradeLists_OpenAndSaveUnderOneSecond()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        AttendanceClass c = await ClassAsync(factory, client);
        List<Guid> students = [];
        for (int i = 1; i <= 40; i++)
        {
            students.Add(await StudentAsync(client, c, $"Öğrenci {i:00}"));
        }

        Guid assignment = await AssignedAsync(client, c.Teacher, c.ClassroomId);
        Guid assessment = await OpenedAsync(client, c.Teacher, c.ClassroomId);
        (await SubmissionsAsync(client, c.Teacher, assignment)).Status.ShouldBe(HttpStatusCode.OK, "ısınma");
        (await GradesAsync(client, c.Teacher, assessment)).Status.ShouldBe(HttpStatusCode.OK, "ısınma");

        List<TimeSpan> openSubmissions = [];
        List<TimeSpan> saveSubmissions = [];
        List<TimeSpan> openGrades = [];
        List<TimeSpan> saveGrades = [];
        for (int run = 0; run < 5; run++)
        {
            Stopwatch watch = Stopwatch.StartNew();
            var (s1, view, _) = await SubmissionsAsync(client, c.Teacher, assignment);
            openSubmissions.Add(watch.Elapsed);
            s1.ShouldBe(HttpStatusCode.OK);
            view.GetProperty("students").GetArrayLength().ShouldBe(40);

            (Guid, string)[] marks = [.. students.Select((id, i) => (id, i % 5 == run ? "NotSubmitted" : "Submitted"))];
            watch.Restart();
            var (s2, _, _) = await SaveSubmissionsAsync(client, c.Teacher, assignment, marks);
            saveSubmissions.Add(watch.Elapsed);
            s2.ShouldBe(HttpStatusCode.OK);

            watch.Restart();
            var (s3, grades, _) = await GradesAsync(client, c.Teacher, assessment);
            openGrades.Add(watch.Elapsed);
            s3.ShouldBe(HttpStatusCode.OK);
            grades.GetProperty("students").GetArrayLength().ShouldBe(40);

            (Guid, object?)[] scores = [.. students.Select((id, i) => (id, (object?)((i + run) % 101)))];
            watch.Restart();
            var (s4, _, _) = await SaveGradesAsync(client, c.Teacher, assessment, scores);
            saveGrades.Add(watch.Elapsed);
            s4.ShouldBe(HttpStatusCode.OK);
        }

        foreach ((string name, List<TimeSpan> values) in new[] { ("teslim listesi", openSubmissions), ("teslim kaydı", saveSubmissions), ("puan listesi", openGrades), ("puan kaydı", saveGrades) })
        {
            Median(values).ShouldBeLessThanOrEqualTo(Limit, $"{name}: {string.Join(", ", values.Select(t => (int)t.TotalMilliseconds))} ms");
        }
    }

    private static TimeSpan Median(List<TimeSpan> values) => values.Order().ElementAt(values.Count / 2);
}
