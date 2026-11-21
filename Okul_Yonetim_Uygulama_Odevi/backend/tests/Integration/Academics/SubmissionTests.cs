using System.Net;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Academics.AcademicsTestData;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Academics;

/// <summary>
/// Spec 0013 AC-4: ödevi veren öğretmen öğrencileri Türkçe ad sırasıyla görür, Teslim etti / Teslim etmedi / İşaretlenmedi
/// seçip tek seferde kaydeder; sonuç sayıları doğru; yeniden açınca kayıtlı durumlar gelir ve düzeltilebilir; sınıftan ayrılan
/// öğrenci listede yoktur ama kaydı velinin özetinde kalır. Geçersiz durum ya da ödevde olmayan öğrenci hiçbir şey yazmaz.
/// </summary>
[Trait("Category", "Integration")]
public sealed class SubmissionTests
{
    private readonly TestDatabase database;

    public SubmissionTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Save_TurkishOrder_CountsReopenAndCorrect()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid cem = await StudentAsync(client, w.Class, "Çağla Demir");
        Guid irem = await StudentAsync(client, w.Class, "İrem Su");
        Guid ozan = await StudentAsync(client, w.Class, "Özge Ak");
        Guid id = await AssignedAsync(client, w.Class.Teacher, w.Class.ClassroomId);

        JsonElement view = (await SubmissionsAsync(client, w.Class.Teacher, id)).Body;
        Names(view).ShouldBe(["Ali Yılmaz", "Ayşe Kaya", "Çağla Demir", "İrem Su", "Özge Ak"], "Türkçe alfabe: A, Ç, İ, Ö");
        view.GetProperty("canEdit").GetBoolean().ShouldBeTrue();
        view.GetProperty("subject").GetString().ShouldBe("Matematik");

        var (status, result, raw) = await SaveSubmissionsAsync(client, w.Class.Teacher, id, (w.Ali, "Submitted"), (w.Ayse, "NotSubmitted"), (cem, "Submitted"), (irem, "Unmarked"), (ozan, "NotSubmitted"));
        status.ShouldBe(HttpStatusCode.OK, raw);
        result.GetProperty("submitted").GetInt32().ShouldBe(2);
        result.GetProperty("notSubmitted").GetInt32().ShouldBe(2);
        result.GetProperty("unmarked").GetInt32().ShouldBe(1);
        result.GetProperty("changed").GetInt32().ShouldBe(4);

        Dictionary<Guid, string?> saved = ByStudent((await SubmissionsAsync(client, w.Class.Teacher, id)).Body, "status");
        saved[w.Ali].ShouldBe("Submitted");
        saved[w.Ayse].ShouldBe("NotSubmitted");
        saved[irem].ShouldBe("Unmarked");

        var (_, corrected, _) = await SaveSubmissionsAsync(client, w.Class.Teacher, id, (w.Ayse, "Submitted"));
        corrected.GetProperty("submitted").GetInt32().ShouldBe(3, "gönderilmeyen öğrencinin durumu değişmez");
        corrected.GetProperty("changed").GetInt32().ShouldBe(1);
        ByStudent((await SubmissionsAsync(client, w.Class.Teacher, id)).Body, "status")[w.Ayse].ShouldBe("Submitted");
    }

    [Fact]
    public async Task StudentLeavesClass_HiddenFromList_RecordStaysInGuardianSummary()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid id = await AssignedAsync(client, w.Class.Teacher, w.Class.ClassroomId);
        (await SaveSubmissionsAsync(client, w.Class.Teacher, id, (w.Ali, "NotSubmitted"))).Status.ShouldBe(HttpStatusCode.OK);

        await MoveAsync(client, w.Class.Founder, w.Ali, "Ali Yılmaz", w.Class.School.BranchId, w.OtherClassroomId);

        JsonElement view = (await SubmissionsAsync(client, w.Class.Teacher, id)).Body;
        Names(view).ShouldBe(["Ayşe Kaya"]);
        (await AssignmentsAsync(client, w.Class.Teacher, w.Class.ClassroomId)).Body[0].GetProperty("studentCount").GetInt32().ShouldBe(1);
        var (_, result, _) = await SaveSubmissionsAsync(client, w.Class.Teacher, id, (w.Ali, "Submitted"), (w.Ayse, "Submitted"));
        result.GetProperty("submitted").GetInt32().ShouldBe(1, "ayrılan öğrencinin satırı yok sayılır");

        JsonElement summary = (await SummaryAsync(client, w.AliMother.Token, w.Ali)).Body;
        JsonElement pending = summary.GetProperty("pending")[0];
        pending.GetProperty("assignmentId").GetGuid().ShouldBe(id);
        pending.GetProperty("status").GetString().ShouldBe("NotSubmitted", "kayıt silinmez, değişmez");
    }

    [Fact]
    public async Task Save_InvalidStatusOrForeignStudent_400AndNothingChanges()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid id = await AssignedAsync(client, w.Class.Teacher, w.Class.ClassroomId);

        var (invalid, problem, _) = await SaveSubmissionsAsync(client, w.Class.Teacher, id, (w.Ali, "Submitted"), (w.Ayse, "Late"));
        var (foreign, problem2, _) = await SaveSubmissionsAsync(client, w.Class.Teacher, id, (w.Ali, "Submitted"), (w.Berk, "Submitted"));
        var (repeated, _, _) = await SaveSubmissionsAsync(client, w.Class.Teacher, id, (w.Ali, "Submitted"), (w.Ali, "NotSubmitted"));

        invalid.ShouldBe(HttpStatusCode.BadRequest);
        Messages(problem).ShouldContain("Durum Teslim etti, Teslim etmedi ya da İşaretlenmedi olmalı");
        foreign.ShouldBe(HttpStatusCode.BadRequest);
        Detail(problem2).ShouldBe("Listede bu kayda ait olmayan bir öğrenci var; listeyi yenileyin.");
        repeated.ShouldBe(HttpStatusCode.BadRequest);
        ByStudent((await SubmissionsAsync(client, w.Class.Teacher, id)).Body, "status").Values.ShouldAllBe(s => s == "Unmarked");
    }
}
