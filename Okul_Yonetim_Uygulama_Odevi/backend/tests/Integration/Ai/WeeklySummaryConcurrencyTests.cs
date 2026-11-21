using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Persistence.Repositories;
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
/// gönderimden (iki öğretmen; yarış kapıyla belirlenimci) sonra her veliye tek özet gitmiş olur ve reddedilenin hata gövdesi kendi
/// mesajını taşır (T19); gönderilmiş
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
        // Belirlenimci yarış (0012 DraftHoldGate kalıbı): birinci gönderim bildirimleri yazıp taslağı onaylamak üzere okuduktan
        // sonra bekletilir; bu sırada ikinci öğretmenin gönderimi başlar. Öğrenci-hafta sırası varsa ikincisi sırada bekler ve
        // onaylanmış taslağı görür; yoksa iki gönderim de bildirim yazmaya çalışır.
        DraftHoldGate gate = new();
        using OkulApiFactory factory = Factory(this.database, new FakeAiTextGenerator(), configure: services =>
        {
            services.RemoveAll<IAiDraftRepository>();
            services.AddScoped<AiDraftRepository>();
            services.AddScoped<IAiDraftRepository>(sp => new GatedAiDraftRepository(sp.GetRequiredService<AiDraftRepository>(), gate));
        });
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (Guid secondId, _, string second) = await TeacherAsync(factory, client, w.Class.School.TenantId, w.Class.School.BranchId);
        await AssignAsync(client, w.Class.Founder, w.Class.ClassroomId, w.Class.TeacherId, secondId);
        await AttendAsync(client, w);
        await GeneratedAsync(client, w.Class.Teacher, w.Ali);

        gate.Enabled = true;
        var first = SendSummaryAsync(client, w.Class.Teacher, w.Ali, "Birinci öğretmenin metni.");
        await gate.Held.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
        var secondSend = SendSummaryAsync(client, second, w.Ali, "İkinci öğretmenin metni.");
        await Task.WhenAny(secondSend, Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
        gate.Release();
        var results = new[] { await first, await secondSend };

        results[0].Status.ShouldBe(HttpStatusCode.OK, results[0].Raw);
        results[1].Status.ShouldBe(HttpStatusCode.BadRequest, results[1].Raw);
        Detail(results[1].Body).ShouldBe(AlreadySent, "reddedilenin gövdesi kendi mesajını taşır (T19)");
        List<GuardianNotice> notices = await SummaryNoticesAsync(factory, w.Class.School.TenantId);
        notices.GroupBy(n => n.GuardianUserId).ShouldAllBe(g => g.Count() == 1);
        notices.Select(n => n.GuardianUserId).ShouldBe([w.AliMother.UserId, w.AliFather.UserId], ignoreOrder: true);
        notices.ShouldAllBe(n => n.Text!.StartsWith("Birinci öğretmenin metni.", StringComparison.Ordinal));
        var draft = (await SummaryDraftsAsync(factory, w.Class.School.TenantId)).ShouldHaveSingleItem();
        draft.FinalText.ShouldBe("Birinci öğretmenin metni.");
        draft.ApprovedByUserId.ShouldBe(w.Class.TeacherId);
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
