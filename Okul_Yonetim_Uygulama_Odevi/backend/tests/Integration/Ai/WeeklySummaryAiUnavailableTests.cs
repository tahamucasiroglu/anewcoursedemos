using System.Net;
using OkulYonetim.Application.Ports;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Ai.WeeklySummaryTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Ai;

/// <summary>
/// Spec 0014 AC-11 (AI kısmı; C-6): metin servisi ayarsız (Testing ortamındaki gerçek kayıt — Pi varsayılanı), kapalı, zaman aşımı ya da
/// boş yanıt verdiğinde "Yapay zekâ şu an kullanılamıyor. Daha sonra tekrar deneyin." döner, taslak oluşmaz, nedeniyle Warning log
/// yazılır; var olan bekleyen taslak korunur (metni değişmez) ve yine gönderilebilir.
/// </summary>
[Trait("Category", "Integration")]
public sealed class WeeklySummaryAiUnavailableTests
{
    private readonly TestDatabase database;

    public WeeklySummaryAiUnavailableTests(TestDatabase database)
    {
        this.database = database;
    }

    public static TheoryData<string> Reasons => ["timeout", "unreachable", "http-503", "empty"];

    [Fact]
    public async Task NotConfigured_DefaultRegistration_Unavailable()
    {
        using OkulApiFactory factory = Factory(this.database, text: null);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await AttendAsync(client, w);

        var (status, problem, raw) = await GenerateAsync(client, w.Class.Teacher, w.Ali);

        status.ShouldBe(HttpStatusCode.BadRequest, raw);
        Detail(problem).ShouldBe(Unavailable);
        (await SummaryDraftsAsync(factory, w.Class.School.TenantId)).ShouldBeEmpty();
        AssertWarning(factory, "not-configured", w.Ali);
    }

    [Theory]
    [MemberData(nameof(Reasons))]
    public async Task ServiceFails_UnavailableExistingDraftKeptAndSendable(string reason)
    {
        FakeAiTextGenerator text = new() { Reply = "Önceki bekleyen taslak." };
        using OkulApiFactory factory = Factory(this.database, text);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await AttendAsync(client, w);
        await GeneratedAsync(client, w.Class.Teacher, w.Ali);
        if (reason == "empty")
        {
            text.Reply = "  ";
        }
        else
        {
            text.Failure = new AiUnavailableException(reason);
        }

        var (status, problem, raw) = await GenerateAsync(client, w.Class.Teacher, w.Ali, "Yeni not.");
        status.ShouldBe(HttpStatusCode.BadRequest, raw);
        Detail(problem).ShouldBe(Unavailable);
        var (_, ayseProblem, _) = await GenerateAsync(client, w.Class.Teacher, w.Ayse);
        Detail(ayseProblem).ShouldBe(Unavailable);

        var draft = (await SummaryDraftsAsync(factory, w.Class.School.TenantId)).ShouldHaveSingleItem("Ayşe için taslak oluşmaz");
        draft.StudentId.ShouldBe(w.Ali);
        draft.IsPending.ShouldBeTrue();
        draft.DraftText.ShouldBe("Önceki bekleyen taslak.", "var olan taslak korunur");
        draft.SourceText.ShouldNotContain("Yeni not.");
        AssertWarning(factory, reason, w.Ali);

        (await SendSummaryAsync(client, w.Class.Teacher, w.Ali, "Önceki bekleyen taslak, düzenlendi.")).Status.ShouldBe(HttpStatusCode.OK, "AI kapalıyken de var olan taslak gönderilir");
        (await SummaryNoticesAsync(factory, w.Class.School.TenantId)).Count.ShouldBe(2);
    }

    private static void AssertWarning(OkulApiFactory factory, string reason, Guid studentId)
    {
        string warning = factory.ReadLog().Split('\n').First(l => l.Contains("AI kullanılamıyor (haftalık özet)", StringComparison.Ordinal) && l.Contains(studentId.ToString(), StringComparison.Ordinal));
        warning.ShouldContain("[WRN]");
        warning.ShouldContain(reason);
    }
}
