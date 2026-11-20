using System.Net;
using OkulYonetim.Domain.Ai;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Ai.AiTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Ai;

/// <summary>
/// Spec 0012 AC-1: ilişkisi süren konuşmanın öğretmeni sesli not gönderince yanıtta taslak kimliği, döküm, taslak ve süre
/// döner; taslak kaydı öğretmen, konuşma, öğrenci, döküm (kaynak) ve taslak metniyle "bekliyor" durumunda oluşur; ses
/// servisine gönderilen ses ve türü aynen gider; model taslağı 1000 karakteri aşarsa 1000'e kısaltılır.
/// </summary>
[Trait("Category", "Integration")]
public sealed class VoiceMessageDraftTests
{
    private readonly TestDatabase database;

    public VoiceMessageDraftTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Create_TeacherOngoingConversation_ReturnsTranscriptAndDraft_StoresPendingDraft()
    {
        FakeSpeechToText speech = new() { Text = "ali bugün matematik ödevini getirmedi", DurationSeconds = 21.4 };
        FakeAiTextGenerator text = new();
        using OkulApiFactory factory = Factory(this.database, speech, text);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid conversation = await StartedAsync(client, w.Class.Teacher, w.Ali, w.AliMother.UserId);

        var (status, body, raw) = await VoiceNoteAsync(client, w.Class.Teacher, conversation, Audio(100_000), "audio/webm;codecs=opus");

        status.ShouldBe(HttpStatusCode.OK, raw);
        Guid draftId = body.GetProperty("draftId").GetGuid();
        body.GetProperty("transcript").GetString().ShouldBe("ali bugün matematik ödevini getirmedi");
        body.GetProperty("draft").GetString().ShouldBe(FakeAiTextGenerator.DefaultReply);
        body.GetProperty("durationSeconds").GetDouble().ShouldBe(21.4);
        speech.LastLength.ShouldBe(100_000);
        speech.LastContentType!.Replace(" ", string.Empty, StringComparison.Ordinal).ShouldBe("audio/webm;codecs=opus");

        AiDraft draft = (await DraftsAsync(factory, w.Class.School.TenantId)).ShouldHaveSingleItem();
        draft.Id.ShouldBe(draftId);
        draft.Kind.ShouldBe(AiDraftKind.VoiceMessage);
        draft.Status.ShouldBe(AiDraftStatus.Pending);
        draft.CreatedByUserId.ShouldBe(w.Class.TeacherId);
        draft.ContextId.ShouldBe(conversation);
        draft.StudentId.ShouldBe(w.Ali);
        draft.SourceText.ShouldBe("ali bugün matematik ödevini getirmedi");
        draft.DraftText.ShouldBe(FakeAiTextGenerator.DefaultReply);
        draft.CreatedAt.ShouldBe(Morning());
        draft.FinalText.ShouldBeNull();
        draft.ResultId.ShouldBeNull();
    }

    [Fact]
    public async Task Create_ModelDraftOver1000_ShortenedTo1000()
    {
        FakeAiTextGenerator text = new() { Reply = "Sayın Veli, " + new string('a', 1500) };
        using OkulApiFactory factory = Factory(this.database, new FakeSpeechToText(), text);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid conversation = await StartedAsync(client, w.Class.Teacher, w.Ali, w.AliMother.UserId);

        var (status, body, raw) = await VoiceNoteAsync(client, w.Class.Teacher, conversation);

        status.ShouldBe(HttpStatusCode.OK, raw);
        body.GetProperty("draft").GetString()!.Length.ShouldBe(1000);
        (await DraftsAsync(factory, w.Class.School.TenantId)).ShouldHaveSingleItem().DraftText.Length.ShouldBe(1000);
    }

    private static DateTimeOffset Morning() => Attendance.AttendanceTestData.Morning;
}
