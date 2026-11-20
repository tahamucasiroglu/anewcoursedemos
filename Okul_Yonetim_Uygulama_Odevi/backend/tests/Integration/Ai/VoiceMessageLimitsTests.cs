using System.Net;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Ai.AiTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Ai;

/// <summary>
/// Spec 0012 AC-5: boş/eksik dosya, desteklenmeyen tür ve 2 MB üstü dosya alan hatası verir (tam 2 MB kabul); 60 saniyeden
/// uzun not "Sesli not en fazla 60 saniye olabilir." ile reddedilir (60,0 sn kabul); döküm boşsa "Ses anlaşılamadı, lütfen
/// tekrar deneyin."; hiçbirinde taslak oluşmaz ve metin servisi çağrılmaz.
/// </summary>
[Trait("Category", "Integration")]
public sealed class VoiceMessageLimitsTests
{
    private const int TwoMegabytes = 2 * 1024 * 1024;

    private readonly TestDatabase database;

    public VoiceMessageLimitsTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task File_EmptyMissingWrongTypeOrOver2Mb_FieldError_Exactly2MbAccepted()
    {
        FakeSpeechToText speech = new();
        FakeAiTextGenerator text = new();
        using OkulApiFactory factory = Factory(this.database, speech, text);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid conversation = await StartedAsync(client, w.Class.Teacher, w.Ali, w.AliMother.UserId);

        var (emptyStatus, empty, _) = await VoiceNoteAsync(client, w.Class.Teacher, conversation, []);
        var (missingStatus, missing, _) = await VoiceNoteAsync(client, w.Class.Teacher, conversation, withFile: false);
        var (typeStatus, type, _) = await VoiceNoteAsync(client, w.Class.Teacher, conversation, Audio(1000), "text/plain");
        var (bigStatus, big, bigRaw) = await VoiceNoteAsync(client, w.Class.Teacher, conversation, Audio(TwoMegabytes + 1));

        emptyStatus.ShouldBe(HttpStatusCode.BadRequest);
        Messages(empty).ShouldContain("Ses kaydı boş olamaz");
        missingStatus.ShouldBe(HttpStatusCode.BadRequest);
        Messages(missing).ShouldContain("Ses kaydı boş olamaz");
        typeStatus.ShouldBe(HttpStatusCode.BadRequest);
        Messages(type).ShouldContain("Ses türü desteklenmiyor (m4a, aac, webm, wav, ogg)");
        bigStatus.ShouldBe(HttpStatusCode.BadRequest, bigRaw);
        Messages(big).ShouldContain("Ses kaydı en fazla 2 MB olabilir");
        speech.Calls.ShouldBe(0);
        text.Requests.ShouldBeEmpty();
        (await DraftsAsync(factory, w.Class.School.TenantId)).ShouldBeEmpty();

        foreach (string accepted in new[] { "audio/m4a", "audio/aac", "audio/webm", "audio/wav", "audio/ogg" })
        {
            (await VoiceNoteAsync(client, w.Class.Teacher, conversation, Audio(1000), accepted)).Status.ShouldBe(HttpStatusCode.OK, accepted);
        }

        var (exactStatus, _, exactRaw) = await VoiceNoteAsync(client, w.Class.Teacher, conversation, Audio(TwoMegabytes));
        exactStatus.ShouldBe(HttpStatusCode.OK, exactRaw);
        speech.LastLength.ShouldBe(TwoMegabytes);
    }

    [Theory]
    [InlineData(60.0, true)]
    [InlineData(60.01, false)]
    [InlineData(95.0, false)]
    public async Task Duration_Over60Seconds_Rejected_60Accepted(double seconds, bool accepted)
    {
        FakeAiTextGenerator text = new();
        using OkulApiFactory factory = Factory(this.database, new FakeSpeechToText { DurationSeconds = seconds }, text);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid conversation = await StartedAsync(client, w.Class.Teacher, w.Ali, w.AliMother.UserId);

        var (status, body, raw) = await VoiceNoteAsync(client, w.Class.Teacher, conversation);

        if (accepted)
        {
            status.ShouldBe(HttpStatusCode.OK, raw);
            (await DraftsAsync(factory, w.Class.School.TenantId)).ShouldHaveSingleItem();
            return;
        }

        status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(body).ShouldBe("Sesli not en fazla 60 saniye olabilir.");
        text.Requests.ShouldBeEmpty();
        (await DraftsAsync(factory, w.Class.School.TenantId)).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Transcript_Empty_NotUnderstoodNoDraft(string transcript)
    {
        FakeAiTextGenerator text = new();
        using OkulApiFactory factory = Factory(this.database, new FakeSpeechToText { Text = transcript, DurationSeconds = 4 }, text);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid conversation = await StartedAsync(client, w.Class.Teacher, w.Ali, w.AliMother.UserId);

        var (status, body, _) = await VoiceNoteAsync(client, w.Class.Teacher, conversation);

        status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(body).ShouldBe("Ses anlaşılamadı, lütfen tekrar deneyin.");
        text.Requests.ShouldBeEmpty();
        (await DraftsAsync(factory, w.Class.School.TenantId)).ShouldBeEmpty();
    }
}
