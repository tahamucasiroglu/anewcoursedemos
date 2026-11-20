using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OkulYonetim.Application.Ports;
using OkulYonetim.Infrastructure.Ai;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Ai.AiTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Ai;

/// <summary>
/// Spec 0012 AC-6: ses ya da metin servisinin adresi boş (Testing ortamındaki gerçek kayıt — Pi varsayılanı), servis ulaşılamaz
/// (gerçek adaptör, kapalı yerel port), zaman aşımı, hata ya da boş model yanıtı durumlarının her birinde "Yapay zekâ şu an
/// kullanılamıyor. Mesajı elle yazabilirsiniz." döner, taslak oluşmaz, nedeniyle Warning log yazılır; aynı konuşmada elle
/// mesaj gönderimi çalışır.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AiUnavailableTests
{
    private const string Unavailable = "Yapay zekâ şu an kullanılamıyor. Mesajı elle yazabilirsiniz.";

    private readonly TestDatabase database;

    public AiUnavailableTests(TestDatabase database)
    {
        this.database = database;
    }

    public static TheoryData<string, string> FailingServices => new()
    {
        { "speech", "timeout" },
        { "speech", "http-503" },
        { "text", "timeout" },
        { "text", "unreachable" },
        { "text", "empty" },
    };

    [Fact]
    public async Task AddressesEmpty_DefaultRegistration_UnavailableAndManualSendWorks()
    {
        using OkulApiFactory factory = Factory(this.database, speech: null, text: null);
        await AssertUnavailableAsync(factory, "not-configured");
    }

    [Fact]
    public async Task SpeechServiceUnreachable_RealAdapter_Unavailable()
    {
        using OkulApiFactory factory = Factory(this.database, speech: null, new FakeAiTextGenerator(), configure: services =>
        {
            services.RemoveAll<ISpeechToText>();
            services.AddHttpClient<ISpeechToText, WhisperServiceSpeechToText>(c =>
            {
                c.BaseAddress = new Uri("http://127.0.0.1:9/");
                c.Timeout = TimeSpan.FromSeconds(5);
            });
        });
        await AssertUnavailableAsync(factory, "unreachable");
    }

    [Theory]
    [MemberData(nameof(FailingServices))]
    public async Task ServiceFails_Unavailable(string service, string reason)
    {
        FakeSpeechToText speech = new();
        FakeAiTextGenerator text = new();
        if (service == "speech")
        {
            speech.Failure = new AiUnavailableException(reason);
        }
        else if (reason == "empty")
        {
            text.Reply = "   ";
        }
        else
        {
            text.Failure = new AiUnavailableException(reason);
        }

        using OkulApiFactory factory = Factory(this.database, speech, text);
        await AssertUnavailableAsync(factory, reason);
    }

    private static async Task AssertUnavailableAsync(OkulApiFactory factory, string reason)
    {
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid conversation = await StartedAsync(client, w.Class.Teacher, w.Ali, w.AliMother.UserId);

        var (status, problem, raw) = await VoiceNoteAsync(client, w.Class.Teacher, conversation);

        status.ShouldBe(HttpStatusCode.BadRequest, raw);
        Detail(problem).ShouldBe(Unavailable);
        (await DraftsAsync(factory, w.Class.School.TenantId)).ShouldBeEmpty();
        string warning = factory.ReadLog().Split('\n').Where(l => l.Contains("AI kullanılamıyor", StringComparison.Ordinal)).ShouldHaveSingleItem();
        warning.ShouldContain("[WRN]");
        warning.ShouldContain(reason);
        warning.ShouldContain(conversation.ToString());

        (await SendAsync(client, w.Class.Teacher, conversation, "Elle yazdığım mesaj")).Status.ShouldBe(HttpStatusCode.OK);
        (await MessageCountAsync(factory, conversation)).ShouldBe(1);
    }
}
