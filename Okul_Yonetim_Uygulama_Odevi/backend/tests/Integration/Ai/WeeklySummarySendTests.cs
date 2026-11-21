using System.Net;
using System.Text.Json;
using OkulYonetim.Domain.Ai;
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
/// Spec 0014 AC-6 (BR-5, BR-6): onaylanan (düzenlenmiş) metin, öğrencinin okulda etkin her velisine "Haftalık gelişim özeti — 16–22
/// Kasım" başlıklı, "Yapay zekâ desteğiyle hazırlandı, öğretmen onayladı." etiketli ve dayanak satırlı birer okul bildirimi olarak gider —
/// bildirim kapısı veli başına bir kez, pasif veliye gitmez; taslak son metin ve onaylayanla (sınıfın başka atanmış öğretmeni de
/// olabilir) "onaylandı" olur; etkin velisi olmayan öğrencide "Bu öğrencinin kayıtlı velisi yok." ve hiçbir şey değişmez.
/// </summary>
[Trait("Category", "Integration")]
public sealed class WeeklySummarySendTests
{
    private readonly TestDatabase database;

    public WeeklySummarySendTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Send_EditedText_EachActiveGuardianOnceWithLabelAndBasis_DraftApproved()
    {
        FakeAiTextGenerator text = new() { Reply = "Modelin taslağı." };
        CapturingPushNotifier notifier = new();
        using OkulApiFactory factory = Factory(this.database, text, notifier);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        ParentAccount aunt = await ParentAsync(client, w.Class, w.Ali, "Other");
        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/members/{aunt.UserId}/deactivate", w.Class.Founder)).ShouldBe(HttpStatusCode.OK);
        (Guid secondId, _, string second) = await TeacherAsync(factory, client, w.Class.School.TenantId, w.Class.School.BranchId);
        await AssignAsync(client, w.Class.Founder, w.Class.ClassroomId, w.Class.TeacherId, secondId);
        await AttendAsync(client, w, (w.Ali, "Late"));
        await GeneratedAsync(client, w.Class.Teacher, w.Ali);
        int pushesBefore = notifier.Sent.Count;
        const string Edited = "Ali bu hafta bir gün geç kaldı; derslere istekle katıldı.";

        var (status, sent, raw) = await SendSummaryAsync(client, second, w.Ali, "  " + Edited + "  ");

        status.ShouldBe(HttpStatusCode.OK, raw);
        sent.GetProperty("notifiedGuardians").GetInt32().ShouldBe(2);
        string expected = Edited + "\n\n" + Label + "\nDayanak: 1 gün geldi (1 gün geç), 0 gün gelmedi";
        List<GuardianNotice> notices = await SummaryNoticesAsync(factory, w.Class.School.TenantId);
        notices.Select(n => n.GuardianUserId).ShouldBe([w.AliMother.UserId, w.AliFather.UserId], ignoreOrder: true);
        notices.ShouldAllBe(n => n.StudentId == w.Ali && n.Text == expected && n.Day == new DateOnly(2026, 11, 16));
        AiDraft draft = (await SummaryDraftsAsync(factory, w.Class.School.TenantId)).ShouldHaveSingleItem();
        notices.ShouldAllBe(n => n.SourceId == draft.Id);
        notifier.Sent.Skip(pushesBefore).Select(p => p.RecipientUserId).ShouldBe([w.AliMother.UserId, w.AliFather.UserId], ignoreOrder: true);
        notifier.To(aunt.UserId).ShouldBeEmpty("pasif veliye gitmez");
        draft.Status.ShouldBe(AiDraftStatus.Approved);
        draft.FinalText.ShouldBe(Edited);
        draft.DraftText.ShouldBe("Modelin taslağı.", "model taslağı kaynak olarak kalır");
        draft.ApprovedByUserId.ShouldBe(secondId, "sınıfın atanmış her öğretmeni onaylayabilir");
        draft.ApprovedAt.ShouldNotBeNull();

        JsonElement item = (await FeedAsync(client, w.AliMother.Token)).Body.GetProperty("items").EnumerateArray()
            .Single(i => i.GetProperty("kind").GetString() == "WeeklySummary");
        item.GetProperty("title").GetString().ShouldBe("Haftalık gelişim özeti — " + ThisWeekLabel);
        item.GetProperty("childNames").EnumerateArray().Select(n => n.GetString()).ShouldBe(["Ali Yılmaz"]);
        var (detailStatus, detail, detailRaw) = await NoticeAsync(client, w.AliMother.Token, item.GetProperty("id").GetGuid());
        detailStatus.ShouldBe(HttpStatusCode.OK, detailRaw);
        detail.GetProperty("text").GetString().ShouldBe(expected);
        detail.GetProperty("title").GetString().ShouldBe("Haftalık gelişim özeti — " + ThisWeekLabel);
        detail.GetProperty("kind").GetString().ShouldBe("WeeklySummary");

        JsonElement panel = (await PanelAsync(client, w.Class.Teacher, w.Ali)).Body;
        panel.GetProperty("status").GetString().ShouldBe("Sent");
        panel.GetProperty("finalText").GetString().ShouldBe(Edited);
        panel.GetProperty("draftId").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Send_StudentWithoutActiveGuardian_RejectedNothingChanges()
    {
        FakeAiTextGenerator text = new();
        CapturingPushNotifier notifier = new();
        using OkulApiFactory factory = Factory(this.database, text, notifier);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid lonely = await StudentAsync(client, w.Class, "Deniz Yalnız");
        await SavedAsync(client, w.Class.Teacher, w.Class.ClassroomId, Marks([w.Ali, w.Ayse, lonely]));
        await GeneratedAsync(client, w.Class.Teacher, lonely);
        int pushesBefore = notifier.Sent.Count;

        var (status, problem, raw) = await SendSummaryAsync(client, w.Class.Teacher, lonely, "Deniz bu hafta her gün geldi.");

        status.ShouldBe(HttpStatusCode.BadRequest, raw);
        Detail(problem).ShouldBe(NoGuardian);
        (await SummaryDraftsAsync(factory, w.Class.School.TenantId)).ShouldHaveSingleItem().IsPending.ShouldBeTrue();
        (await SummaryNoticesAsync(factory, w.Class.School.TenantId)).ShouldBeEmpty();
        notifier.Sent.Count.ShouldBe(pushesBefore);
        (await AuditAsync(factory, w.Class.School.TenantId, Domain.Compliance.AuditAction.WeeklySummarySent)).ShouldBeEmpty();
    }
}
