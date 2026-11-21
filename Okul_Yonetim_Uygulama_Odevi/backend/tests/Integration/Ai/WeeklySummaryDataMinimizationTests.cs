using System.Net;
using OkulYonetim.Application.Features.Ai.Prompts;
using OkulYonetim.Application.Ports;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Ai.WeeklySummaryTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Ai;

/// <summary>
/// Spec 0014 AC-5 (S2, veri minimizasyonu): metin servisine giden istek yalnızca sabit talimatı ve öğrencinin adını, dayanağını ve
/// öğretmen notunu taşır; soyadı, sağlık notu, alerji, veli adı/e-postası/telefonu ve aynı sınıftaki başka öğrencilerin adı içermez.
/// Model yalnızca yerel adresle kullanılır (0012 <c>AiSettingsTests</c>); testte sahte servis.
/// </summary>
[Trait("Category", "Integration")]
public sealed class WeeklySummaryDataMinimizationTests
{
    private readonly TestDatabase database;

    public WeeklySummaryDataMinimizationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Generate_RequestCarriesFirstNameBasisAndNoteOnly()
    {
        FakeAiTextGenerator text = new();
        using OkulApiFactory factory = Factory(this.database, text);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid elif = await CreateStudentAsync(client, w.Class.Founder, NewStudent(
            w.Class.School.BranchId, "Elif Gizlisoyadı", classroomId: w.Class.ClassroomId, healthNote: "Gizlisağlık astım", allergy: "Gizlialerji fıstık"));
        string guardianEmail = UniqueEmail("gizliveli");
        await LinkAsync(client, w.Class.Founder, elif, Guardian(guardianEmail, "Gizliveli Annesi", "Mother", "+905551112233"));
        await SavedAsync(client, w.Class.Teacher, w.Class.ClassroomId, Marks([w.Ali, w.Ayse, elif], (elif, "Absent"), (w.Ayse, "Absent")));

        var (status, _, raw) = await GenerateAsync(client, w.Class.Teacher, elif, "Okuma saatinde çok istekliydi.");

        status.ShouldBe(HttpStatusCode.OK, raw);
        AiTextRequest request = text.Requests.ShouldHaveSingleItem();
        request.Instruction.ShouldBe(AiPrompts.WeeklySummary);
        request.MaxTokens.ShouldBe(AiPrompts.WeeklySummaryMaxTokens);
        request.Content.ShouldContain("Öğrenci: Elif");
        request.Content.ShouldContain("1 gün yoklama alındı");
        request.Content.ShouldContain("Gelmediği günler: 20 Kasım Cuma");
        request.Content.ShouldContain("Öğretmen notu: Okuma saatinde çok istekliydi.");
        string everything = request.Instruction + "\n" + request.Content;
        foreach (string secret in new[] { "Gizlisoyadı", "Gizlisağlık", "astım", "Gizlialerji", "fıstık", "Gizliveli", guardianEmail, "5551112233", "Ali", "Yılmaz", "Ayşe", "Kaya", "Berk", w.Class.School.TenantId.ToString(), elif.ToString() })
        {
            everything.ShouldNotContain(secret, customMessage: secret);
        }
    }
}
