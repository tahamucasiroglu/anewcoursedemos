using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OkulYonetim.Domain.Attendance;
using OkulYonetim.Domain.Billing;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Students;
using OkulYonetim.Tests.Integration.Attendance;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Dashboard.DashboardTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Dashboard;

/// <summary>
/// Spec 0017 AC-12 (gereksinim 12; plan Risks 4): 1.000 öğrenci, 40 sınıf (39'unda bugün yoklama), 1.000 plan / 2.000 taksit (hepsi gecikmiş),
/// 200 duyuru (20 alıcı, yarısı okunmuş) ve 10.000 tahsilatlı okulda günlük özetin üç kartı birlikte ≤ 2 sn, 10.000 satırlık tahsilat dışa
/// aktarması ≤ 5 sn — ısınma ölçümü hariç 5 ölçümün ortancası, yerel makinede (test sunucusu bellekte, ağ yok). Veri hızlı olsun diye
/// doğrudan veritabanına yazılır; sınıf ve atamalar API'den. Süreler test çıktısına ve eşik iletisine yazılır.
/// </summary>
[Trait("Category", "Integration")]
[Collection(SpeedMeasurementGroup.Name)]
public sealed class DashboardPerformanceTests
{
    private static readonly TimeSpan SummaryLimit = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan ExportLimit = TimeSpan.FromSeconds(5);
    private readonly TestDatabase database;

    public DashboardPerformanceTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task RealisticSchool_SummaryUnderTwoSeconds_TenThousandRowExportUnderFive()
    {
        using OkulApiFactory factory = DashboardFactory(this.database);
        using HttpClient client = factory.CreateClient();
        AttendanceClass c = await ClassAsync(factory, client);
        Guid tenant = c.School.TenantId;
        Guid branch = c.School.BranchId;
        Guid founderId = c.School.FounderId;
        List<Guid> classrooms = [c.ClassroomId];
        for (int i = 2; i <= 40; i++)
        {
            Guid classroom = await CreateClassroomAsync(client, c.Founder, c.TermId, branch, c.LevelId, $"S-{i:00}");
            await AssignAsync(client, c.Founder, classroom, c.TeacherId);
            classrooms.Add(classroom);
        }

        List<Guid> planIds = [];
        DateOnly today = new(2026, 11, 20);
        await WithContextAsync(factory, context =>
        {
            context.ChangeTracker.AutoDetectChangesEnabled = false;
            FeeSchedule fee = FeeSchedule.Create(tenant, "Yıllık ücret", 20_000m, Morning, founderId);
            context.FeeSchedules.Add(fee);
            for (int i = 0; i < 1_000; i++)
            {
                Guid classroom = classrooms[i % 40];
                Student student = Student.Create(tenant, branch, classroom, $"Öğrenci {i:0000}", new DateOnly(2015, 1, 1), null, Morning, founderId);
                context.Students.Add(student);
                (InstallmentPlan plan, IReadOnlyList<Installment> installments) = InstallmentPlan.Open(
                    tenant, branch, student.Id, fee, null, 2, new DateOnly(2026, 10, 15), new DateOnly(2026, 10, 1), Morning, founderId);
                context.InstallmentPlans.Add(plan);
                context.Installments.AddRange(installments);
                planIds.Add(plan.Id);
            }

            return Task.CompletedTask;
        });

        await WithContextAsync(factory, async context =>
        {
            context.ChangeTracker.AutoDetectChangesEnabled = false;
            Dictionary<Guid, List<Guid>> roster = (await context.Students.IgnoreQueryFilters().AsNoTracking().Where(s => s.TenantId == tenant && s.ClassroomId != null).ToListAsync())
                .GroupBy(s => s.ClassroomId!.Value).ToDictionary(g => g.Key, g => g.Select(s => s.Id).ToList());
            foreach (Guid classroom in classrooms.Skip(1))
            {
                AttendanceSheet sheet = AttendanceSheet.Start(tenant, branch, classroom, today, c.TeacherId, Morning);
                context.AttendanceSheets.Add(sheet);
                List<Guid> students = roster[classroom];
                for (int s = 0; s < students.Count; s++)
                {
                    context.AttendanceEntries.Add(AttendanceEntry.Create(sheet, students[s], s == 0 ? AttendanceStatus.Absent : s == 1 ? AttendanceStatus.Late : AttendanceStatus.Present, Morning));
                }
            }

            List<User> guardians = [.. Enumerable.Range(0, 20).Select(g => User.Create(UniqueEmail("veli"), $"Veli {g:00}", "özet-yok", Morning))];
            context.Users.AddRange(guardians);
            for (int a = 0; a < 200; a++)
            {
                Announcement announcement = Announcement.Publish(tenant, founderId, AnnouncementScope.School, null, $"Duyuru {a:000}", "Metin", null, null, Morning.AddMinutes(-144 * a));
                context.Announcements.Add(announcement);
                for (int g = 0; g < guardians.Count; g++)
                {
                    AnnouncementRecipient recipient = AnnouncementRecipient.For(announcement, guardians[g].Id);
                    if (g % 2 == 0)
                    {
                        recipient.MarkRead(Morning);
                    }

                    context.AnnouncementRecipients.Add(recipient);
                }
            }

        });

        await ReportExportSecurityTests.SeedPaymentsAsync(factory, planIds, founderId, firstReceipt: 1, count: 10_000);

        await SummaryAsync(client, c.Founder, verify: true);
        (await ExportAsync(client, c.Founder, "payments", "startDate=2026-11-01&endDate=2026-11-20")).Status.ShouldBe(HttpStatusCode.OK, "ısınma");

        List<TimeSpan> summary = [];
        List<TimeSpan> export = [];
        for (int run = 0; run < 5; run++)
        {
            Stopwatch watch = Stopwatch.StartNew();
            await SummaryAsync(client, c.Founder, verify: false);
            summary.Add(watch.Elapsed);

            watch.Restart();
            ExportResponse file = await ExportAsync(client, c.Founder, "payments", "startDate=2026-11-01&endDate=2026-11-20");
            export.Add(watch.Elapsed);
            file.Status.ShouldBe(HttpStatusCode.OK);
            Lines(file.Bytes).Length.ShouldBe(1 + 10_000 + 4);
        }

        string text = $"günlük özet (3 kart): {Ms(summary)} ms · 10.000 satır dışa aktarma: {Ms(export)} ms";
        TestContext.Current.TestOutputHelper?.WriteLine(text);
        Median(summary).ShouldBeLessThanOrEqualTo(SummaryLimit, text);
        Median(export).ShouldBeLessThanOrEqualTo(ExportLimit, text);
    }

    private static TimeSpan Median(List<TimeSpan> values) => values.Order().ElementAt(values.Count / 2);

    private static string Ms(List<TimeSpan> values) => string.Join(", ", values.Select(t => (int)t.TotalMilliseconds));

    private static async Task SummaryAsync(HttpClient client, string founder, bool verify)
    {
        var (s1, attendance, _) = await AttendanceCardAsync(client, founder);
        var (s2, overdue, _) = await OverdueCardAsync(client, founder);
        var (s3, unread, _) = await AnnouncementsCardAsync(client, founder);
        s1.ShouldBe(HttpStatusCode.OK);
        s2.ShouldBe(HttpStatusCode.OK);
        s3.ShouldBe(HttpStatusCode.OK);
        if (verify)
        {
            attendance.GetProperty("classroomCount").GetInt32().ShouldBe(40);
            attendance.GetProperty("takenCount").GetInt32().ShouldBe(39);
            attendance.GetProperty("absentCount").GetInt32().ShouldBe(39);
            overdue.GetProperty("studentCount").GetInt32().ShouldBe(1_000);
            overdue.GetProperty("top").GetArrayLength().ShouldBe(10);
            JsonElement top = unread.GetProperty("top");
            top.GetArrayLength().ShouldBe(10);
            unread.GetProperty("announcementCount").GetInt32().ShouldBeGreaterThan(60);
        }
    }
}
