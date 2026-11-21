using System.Net;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Ai.WeeklySummaryTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Ai;

/// <summary>
/// Spec 0014 AC-7: aynı öğrenci-hafta için ikinci gönderim "Bu hafta için özet zaten gönderildi." ile reddedilir; aynı anda gelen iki
/// gönderimden (iki öğretmen) sonra her veliye tek özet gitmiş olur ve reddedilenin hata gövdesi kendi mesajını taşır (T19); gönderilmiş
/// haftada "Özet oluştur" da aynı mesajla reddedilir ve model çağrılmaz; "Vazgeç" bekleyen taslağı siler (gönderilmiş ya da olmayan
/// taslakta 404).
/// </summary>
[Trait("Category", "Integration")]
public sealed class WeeklySummaryConcurrencyTests
{
    private readonly TestDatabase database;

    public WeeklySummaryConcurrencyTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task SecondSendAndGenerateAfterSent_AlreadySent()
    {
        FakeAiTextGenerator text = new();
        using OkulApiFactory factory = Factory(this.database, text);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await AttendAsync(client, w);
        await GeneratedAsync(client, w.Class.Teacher, w.Ali);
        (await SendSummaryAsync(client, w.Class.Teacher, w.Ali, "İlk gönderim.")).Status.ShouldBe(HttpStatusCode.OK);
        int calls = text.Requests.Count;

        var (again, problem, raw) = await SendSummaryAsync(client, w.Class.Teacher, w.Ali, "İkinci gönderim.");
        again.ShouldBe(HttpStatusCode.BadRequest, raw);
        Detail(problem).ShouldBe(AlreadySent);

        var (generate, generateProblem, generateRaw) = await GenerateAsync(client, w.Class.Teacher, w.Ali);
        generate.ShouldBe(HttpStatusCode.BadRequest, generateRaw);
        Detail(generateProblem).ShouldBe(AlreadySent);
        text.Requests.Count.ShouldBe(calls, "gönderilmiş haftada model çağrılmaz");
        (await DiscardSummaryAsync(client, w.Class.Teacher, w.Ali)).Status.ShouldBe(HttpStatusCode.NotFound, "gönderilmiş özet silinmez");

        List<GuardianNotice> notices = await SummaryNoticesAsync(factory, w.Class.School.TenantId);
        notices.Count.ShouldBe(2);
        notices.ShouldAllBe(n => n.Text!.StartsWith("İlk gönderim.", StringComparison.Ordinal));
        (await SummaryDraftsAsync(factory, w.Class.School.TenantId)).ShouldHaveSingleItem().FinalText.ShouldBe("İlk gönderim.");
    }

    [Fact]
    public async Task ConcurrentSends_TwoTeachers_EachGuardianOneSummary()
    {
        using OkulApiFactory factory = Factory(this.database, new FakeAiTextGenerator());
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (Guid secondId, _, string second) = await TeacherAsync(factory, client, w.Class.School.TenantId, w.Class.School.BranchId);
        await AssignAsync(client, w.Class.Founder, w.Class.ClassroomId, w.Class.TeacherId, secondId);
        await AttendAsync(client, w);
        await GeneratedAsync(client, w.Class.Teacher, w.Ali);

        var results = await Task.WhenAll(
            SendSummaryAsync(client, w.Class.Teacher, w.Ali, "Birinci öğretmenin metni."),
            SendSummaryAsync(client, second, w.Ali, "İkinci öğretmenin metni."));

        results.Count(r => r.Status == HttpStatusCode.OK).ShouldBe(1, string.Join(" | ", results.Select(r => r.Raw)));
        var rejected = results.Single(r => r.Status != HttpStatusCode.OK);
        rejected.Status.ShouldBe(HttpStatusCode.BadRequest, rejected.Raw);
        Detail(rejected.Body).ShouldBe(AlreadySent);
        List<GuardianNotice> notices = await SummaryNoticesAsync(factory, w.Class.School.TenantId);
        notices.GroupBy(n => n.GuardianUserId).ShouldAllBe(g => g.Count() == 1);
        notices.Select(n => n.GuardianUserId).ShouldBe([w.AliMother.UserId, w.AliFather.UserId], ignoreOrder: true);
        (await SummaryDraftsAsync(factory, w.Class.School.TenantId)).ShouldHaveSingleItem().IsPending.ShouldBeFalse();
    }

    [Fact]
    public async Task Discard_PendingDraftDeleted_ThenNotFound()
    {
        using OkulApiFactory factory = Factory(this.database, new FakeAiTextGenerator());
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await AttendAsync(client, w);
        await GeneratedAsync(client, w.Class.Teacher, w.Ali, "Not.");

        (await DiscardSummaryAsync(client, w.Class.Teacher, w.Ali)).Status.ShouldBe(HttpStatusCode.NoContent);

        (await SummaryDraftsAsync(factory, w.Class.School.TenantId)).ShouldBeEmpty("dayanağıyla silinir");
        (await PanelAsync(client, w.Class.Teacher, w.Ali)).Body.GetProperty("status").GetString().ShouldBe("None");
        (await DiscardSummaryAsync(client, w.Class.Teacher, w.Ali)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await SendSummaryAsync(client, w.Class.Teacher, w.Ali, "Taslaksız gönderim.")).Status.ShouldBe(HttpStatusCode.NotFound, "bekleyen taslak yoksa gönderilemez");
        (await SummaryNoticesAsync(factory, w.Class.School.TenantId)).ShouldBeEmpty();
    }
}
