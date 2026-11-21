using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Ai;
using OkulYonetim.Persistence.Repositories;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Ai.AiTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Ai;

/// <summary>
/// Spec 0012 AC-2 (BR-5) ve AC-3: taslak oluşunca konuşmada yeni mesaj yoktur ve bildirim kapısı hiç çağrılmamıştır; mesaj
/// yalnızca öğretmen gönderince, öğretmenin düzenlenmiş metniyle oluşur ve veliye bildirim gider; taslak son metin ve mesaj
/// kimliğiyle "onaylandı" olur; aynı taslakla ikinci gönderim (art arda ya da aynı anda) "Bu taslak zaten kullanıldı." ile
/// reddedilir, ikinci mesaj oluşmaz. "Vazgeç" taslağı (döküm dahil) siler; silinmiş taslakla gönderim "bulunamadı". Aynı taslakla eşzamanlı gönder ve vazgeç
/// (review L-2): ya mesaj + onaylı taslak kalır ya da ikisi de yok — mesajı olup taslağı silinmiş durum oluşmaz.
/// </summary>
[Trait("Category", "Integration")]
public sealed class VoiceMessageApprovalTests
{
    private readonly TestDatabase database;

    public VoiceMessageApprovalTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Draft_NothingSentUntilTeacherSends_ThenEditedTextSentAndDraftApproved()
    {
        CapturingPushNotifier notifier = new();
        using OkulApiFactory factory = Factory(this.database, new FakeSpeechToText(), new FakeAiTextGenerator(), notifier);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid conversation = await StartedAsync(client, w.Class.Teacher, w.Ali, w.AliMother.UserId);

        Guid draftId = await DraftedAsync(client, w.Class.Teacher, conversation);

        (await MessageCountAsync(factory, conversation)).ShouldBe(0, "taslak veliye gitmez");
        notifier.Sent.ShouldBeEmpty("bildirim kapısı çağrılmaz");
        (await MessagesAsync(client, w.AliMother.Token, conversation)).Body.GetProperty("messages").GetArrayLength().ShouldBe(0);

        const string edited = "Sayın Veli, Ali bugün matematik ödevini getirmedi. Yarın getirmesini rica ederim. Elif Öğretmen";
        var (status, sent, raw) = await SendDraftAsync(client, w.Class.Teacher, conversation, "  " + edited + "  ", draftId);

        status.ShouldBe(HttpStatusCode.OK, raw);
        Guid messageId = sent.GetProperty("message").GetProperty("id").GetGuid();
        JsonElement message = (await MessagesAsync(client, w.AliMother.Token, conversation)).Body.GetProperty("messages").EnumerateArray().ShouldHaveSingleItem();
        message.GetProperty("body").GetString().ShouldBe(edited);
        message.GetProperty("mine").GetBoolean().ShouldBeFalse();
        notifier.To(w.AliMother.UserId).ShouldHaveSingleItem().Kind.ShouldBe(PushNotificationKind.Message);

        AiDraft draft = (await DraftsAsync(factory, w.Class.School.TenantId)).ShouldHaveSingleItem();
        draft.Status.ShouldBe(AiDraftStatus.Approved);
        draft.FinalText.ShouldBe(edited);
        draft.DraftText.ShouldBe(FakeAiTextGenerator.DefaultReply);
        draft.ResultId.ShouldBe(messageId);
        draft.ApprovedByUserId.ShouldBe(w.Class.TeacherId);
        draft.ApprovedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task SecondSend_SameDraft_RejectedAndNoSecondMessage()
    {
        using OkulApiFactory factory = Factory(this.database, new FakeSpeechToText(), new FakeAiTextGenerator());
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid conversation = await StartedAsync(client, w.Class.Teacher, w.Ali, w.AliMother.UserId);
        Guid draftId = await DraftedAsync(client, w.Class.Teacher, conversation);
        (await SendDraftAsync(client, w.Class.Teacher, conversation, FakeAiTextGenerator.DefaultReply, draftId)).Status.ShouldBe(HttpStatusCode.OK, "düzenlemeden göndermek de onaydır");

        var (status, problem, _) = await SendDraftAsync(client, w.Class.Teacher, conversation, "ikinci deneme", draftId);

        status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(problem).ShouldBe("Bu taslak zaten kullanıldı.");
        (await MessageCountAsync(factory, conversation)).ShouldBe(1);
        (await DraftsAsync(factory, w.Class.School.TenantId)).ShouldHaveSingleItem().FinalText.ShouldBe(FakeAiTextGenerator.DefaultReply);
    }

    [Fact]
    public async Task ConcurrentSends_SameDraft_OnlyOneMessage()
    {
        using OkulApiFactory factory = Factory(this.database, new FakeSpeechToText(), new FakeAiTextGenerator());
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid conversation = await StartedAsync(client, w.Class.Teacher, w.Ali, w.AliMother.UserId);
        Guid draftId = await DraftedAsync(client, w.Class.Teacher, conversation);

        var results = await Task.WhenAll(
            SendDraftAsync(client, w.Class.Teacher, conversation, "birinci", draftId),
            SendDraftAsync(client, w.Class.Teacher, conversation, "ikinci", draftId),
            SendDraftAsync(client, w.Class.Teacher, conversation, "üçüncü", draftId));

        results.Count(r => r.Status == HttpStatusCode.OK).ShouldBe(1);
        results.Where(r => r.Status != HttpStatusCode.OK).ShouldAllBe(r => r.Status == HttpStatusCode.BadRequest && Detail(r.Body) == "Bu taslak zaten kullanıldı.");
        (await MessageCountAsync(factory, conversation)).ShouldBe(1);
    }

    [Fact]
    public async Task InvalidBody_WithDraft_RejectedAndDraftStaysPending()
    {
        using OkulApiFactory factory = Factory(this.database, new FakeSpeechToText(), new FakeAiTextGenerator());
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid conversation = await StartedAsync(client, w.Class.Teacher, w.Ali, w.AliMother.UserId);
        Guid draftId = await DraftedAsync(client, w.Class.Teacher, conversation);

        var (emptyStatus, empty, _) = await SendDraftAsync(client, w.Class.Teacher, conversation, "   ", draftId);
        var (longStatus, _, _) = await SendDraftAsync(client, w.Class.Teacher, conversation, new string('m', 2001), draftId);

        emptyStatus.ShouldBe(HttpStatusCode.BadRequest);
        Messages(empty).ShouldContain("Mesaj boş olamaz");
        longStatus.ShouldBe(HttpStatusCode.BadRequest);
        (await MessageCountAsync(factory, conversation)).ShouldBe(0);
        (await DraftsAsync(factory, w.Class.School.TenantId)).ShouldHaveSingleItem().Status.ShouldBe(AiDraftStatus.Pending);
    }

    [Fact]
    public async Task Discard_DeletesDraftWithTranscript_SendWithItNotFound()
    {
        using OkulApiFactory factory = Factory(this.database, new FakeSpeechToText(), new FakeAiTextGenerator());
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid conversation = await StartedAsync(client, w.Class.Teacher, w.Ali, w.AliMother.UserId);
        Guid draftId = await DraftedAsync(client, w.Class.Teacher, conversation);

        (await DiscardAsync(client, w.Class.Teacher, draftId)).Status.ShouldBe(HttpStatusCode.NoContent);

        (await DraftsAsync(factory, w.Class.School.TenantId)).ShouldBeEmpty("taslak ve dökümü silinir");
        var (status, problem, _) = await SendDraftAsync(client, w.Class.Teacher, conversation, "metin", draftId);
        status.ShouldBe(HttpStatusCode.NotFound);
        Detail(problem).ShouldBe("Taslak bulunamadı");
        (await MessageCountAsync(factory, conversation)).ShouldBe(0);
        (await DiscardAsync(client, w.Class.Teacher, draftId)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await SendAsync(client, w.Class.Teacher, conversation, "Elle yazılmış mesaj")).Status.ShouldBe(HttpStatusCode.OK, "elle gönderim etkilenmez");
    }

    [Fact]
    public async Task ConcurrentSendAndDiscard_SameDraft_MessageWithApprovedDraftOrNeither()
    {
        DraftHoldGate gate = new();
        using OkulApiFactory factory = Factory(this.database, new FakeSpeechToText(), new FakeAiTextGenerator(), configure: services =>
        {
            services.RemoveAll<IAiDraftRepository>();
            services.AddScoped<AiDraftRepository>();
            services.AddScoped<IAiDraftRepository>(sp => new GatedAiDraftRepository(sp.GetRequiredService<AiDraftRepository>(), gate));
        });
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid conversation = await StartedAsync(client, w.Class.Teacher, w.Ali, w.AliMother.UserId);
        Guid draftId = await DraftedAsync(client, w.Class.Teacher, conversation);

        gate.Enabled = true;
        var discard = DiscardAsync(client, w.Class.Teacher, draftId);
        await gate.Held.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
        var send = SendDraftAsync(client, w.Class.Teacher, conversation, "gönderilen metin", draftId);
        await Task.WhenAny(send, Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
        gate.Release();
        var (discardStatus, _, discardRaw) = await discard;
        var (sendStatus, sent, sendRaw) = await send;

        int messages = await MessageCountAsync(factory, conversation);
        List<AiDraft> drafts = await DraftsAsync(factory, w.Class.School.TenantId);
        if (messages == 1)
        {
            sendStatus.ShouldBe(HttpStatusCode.OK, sendRaw);
            discardStatus.ShouldBe(HttpStatusCode.NotFound, discardRaw);
            AiDraft draft = drafts.ShouldHaveSingleItem("mesaj gittiyse onaylı taslak kalır");
            draft.Status.ShouldBe(AiDraftStatus.Approved);
            draft.ResultId.ShouldBe(sent.GetProperty("message").GetProperty("id").GetGuid());
        }
        else
        {
            messages.ShouldBe(0);
            drafts.ShouldBeEmpty("vazgeçildiyse taslak yok");
            discardStatus.ShouldBe(HttpStatusCode.NoContent, discardRaw);
            sendStatus.ShouldNotBe(HttpStatusCode.OK, sendRaw);
        }
    }
}
