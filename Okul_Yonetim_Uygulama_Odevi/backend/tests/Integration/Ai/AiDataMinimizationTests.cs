using System.Net;
using OkulYonetim.Application.Features.Ai.Prompts;
using OkulYonetim.Application.Ports;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Ai.AiTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Ai;

/// <summary>
/// Spec 0012 AC-7 (veri minimizasyonu, S2): metin servisine giden istek yalnızca uygulamanın sabit talimatını ve dökümü
/// içerir — sağlık notu ve alerjisi kayıtlı öğrencinin adı, velinin adı/e-postası, sağlık bilgisi ve öğretmen adı gitmez.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AiDataMinimizationTests
{
    private readonly TestDatabase database;

    public AiDataMinimizationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task TextRequest_OnlyFixedInstructionAndTranscript()
    {
        const string transcript = "bugün derste çok iyiydi ödevlerini de zamanında getirdi";
        FakeAiTextGenerator text = new();
        using OkulApiFactory factory = Factory(this.database, new FakeSpeechToText { Text = transcript, DurationSeconds = 9 }, text);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid student = await StudentAsync(client, w.Class, "Gizliadlı Öğrencisoyadı");
        ParentAccount parent = await ParentAsync(client, w.Class, student);
        (await StatusAsync(client, HttpMethod.Put, $"/api/admin/students/{student}/health", w.Class.Founder, new { healthNote = "Gizlisağlık astım", allergy = "Gizlialerji fıstık" })).ShouldBe(HttpStatusCode.OK);
        Guid conversation = await StartedAsync(client, w.Class.Teacher, student, parent.UserId);

        (await VoiceNoteAsync(client, w.Class.Teacher, conversation)).Status.ShouldBe(HttpStatusCode.OK);

        AiTextRequest request = text.Requests.ShouldHaveSingleItem();
        request.Instruction.ShouldBe(AiPrompts.VoiceMessage);
        request.Content.ShouldBe(transcript);
        request.MaxTokens.ShouldBe(AiPrompts.VoiceMessageMaxTokens);
        string sent = request.Instruction + "\n" + request.Content;
        foreach (string secret in new[] { "Gizliadlı", "Öğrencisoyadı", "Veli Kişi", parent.Email, "Gizlisağlık", "astım", "Gizlialerji", "fıstık", w.Class.TeacherEmail })
        {
            sent.ShouldNotContain(secret, Case.Insensitive, secret);
        }
    }
}
