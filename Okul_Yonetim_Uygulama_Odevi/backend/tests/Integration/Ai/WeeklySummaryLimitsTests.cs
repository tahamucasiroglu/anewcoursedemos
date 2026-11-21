using System.Net;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Ai.WeeklySummaryTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Ai;

/// <summary>
/// Spec 0014 AC-8 (sınır değerleri): öğretmen notu 500 karakterde kabul, 501'de <c>TeacherNote</c> alan hatası (Türkçe, gerçek gövde
/// biçimi — T20) ve model çağrılmaz; onay metni boşsa ya da 1500'ü aşarsa <c>Text</c> alan hatası, 1500 kabul; hatada hiçbir şey
/// değişmez (taslak bekliyor, bildirim yok).
/// </summary>
[Trait("Category", "Integration")]
public sealed class WeeklySummaryLimitsTests
{
    private readonly TestDatabase database;

    public WeeklySummaryLimitsTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task TeacherNote_500Accepted_501FieldErrorNothingWritten()
    {
        FakeAiTextGenerator text = new();
        using OkulApiFactory factory = Factory(this.database, text);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);

        var (tooLong, problem, raw) = await GenerateAsync(client, w.Class.Teacher, w.Ali, new string('n', 501));
        tooLong.ShouldBe(HttpStatusCode.BadRequest, raw);
        AssertField(problem, "TeacherNote", "Öğretmen notu en fazla 500 karakter olabilir");
        text.Requests.ShouldBeEmpty();
        (await SummaryDraftsAsync(factory, w.Class.School.TenantId)).ShouldBeEmpty();

        var (ok, panel, okRaw) = await GenerateAsync(client, w.Class.Teacher, w.Ali, new string('n', 500));
        ok.ShouldBe(HttpStatusCode.OK, okRaw);
        panel.GetProperty("basis").GetProperty("teacherNote").GetString()!.Length.ShouldBe(500);
    }

    [Fact]
    public async Task SendText_EmptyOrOver1500_FieldError_1500Accepted()
    {
        using OkulApiFactory factory = Factory(this.database, new FakeAiTextGenerator());
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await AttendAsync(client, w);
        await GeneratedAsync(client, w.Class.Teacher, w.Ali);

        foreach (string? bad in new[] { null, string.Empty, "   " })
        {
            var (status, problem, raw) = await SendSummaryAsync(client, w.Class.Teacher, w.Ali, bad);
            status.ShouldBe(HttpStatusCode.BadRequest, raw);
            AssertField(problem, "Text", "Özet metni boş olamaz");
        }

        var (over, overProblem, overRaw) = await SendSummaryAsync(client, w.Class.Teacher, w.Ali, new string('m', 1501));
        over.ShouldBe(HttpStatusCode.BadRequest, overRaw);
        AssertField(overProblem, "Text", "Özet metni en fazla 1500 karakter olabilir");
        (await SummaryDraftsAsync(factory, w.Class.School.TenantId)).ShouldHaveSingleItem().IsPending.ShouldBeTrue("hatada hiçbir şey değişmez");
        (await SummaryNoticesAsync(factory, w.Class.School.TenantId)).ShouldBeEmpty();

        var (ok, _, okRaw) = await SendSummaryAsync(client, w.Class.Teacher, w.Ali, new string('m', 1500));
        ok.ShouldBe(HttpStatusCode.OK, okRaw);
        (await SummaryDraftsAsync(factory, w.Class.School.TenantId)).ShouldHaveSingleItem().FinalText!.Length.ShouldBe(1500);
    }

    private static void AssertField(JsonElement problem, string property, string message)
    {
        Detail(problem).ShouldBe(message);
        JsonElement field = problem.GetProperty("Errors").EnumerateArray().ShouldHaveSingleItem();
        field.GetProperty("Property").GetString().ShouldBe(property);
        field.GetProperty("Errors")[0].GetString().ShouldBe(message);
    }
}
