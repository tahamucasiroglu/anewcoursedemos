using System.Collections.Concurrent;
using System.Net;
using Microsoft.EntityFrameworkCore;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Ai.AiTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Ai;

/// <summary>
/// Spec 0012 AC-8 (gizlilik, S2): taslak tablosunda ses kolonu (ikili veri) yoktur ve kayıtta sesin baytları geçmez;
/// sesli not isteği ASP.NET'in geçici klasörüne ses dosyası yazmaz (<c>ASPNETCORE_*.tmp</c> açılmaz — form tamponu bellekte);
/// log satırlarında döküm, taslak ve gönderilen mesaj metni geçmez — yalnızca kimlikler, süre ve karakter sayıları.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AiPrivacyTests
{
    private readonly TestDatabase database;

    public AiPrivacyTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Schema_NoAudioColumn()
    {
        using OkulApiFactory factory = Factory(this.database, new FakeSpeechToText(), new FakeAiTextGenerator());
        using HttpClient client = factory.CreateClient();
        await WorldAsync(factory, client);

        List<string> columns = await QueryAsync(factory, c => c.Database
            .SqlQuery<string>($"SELECT column_name || ':' || data_type AS \"Value\" FROM information_schema.columns WHERE table_name = 'ai_drafts'")
            .ToListAsync());

        columns.ShouldNotBeEmpty();
        columns.ShouldAllBe(c => !c.EndsWith(":bytea", StringComparison.Ordinal) && !c.Contains("audio", StringComparison.Ordinal) && !c.Contains("voice", StringComparison.Ordinal));
    }

    [Fact]
    public async Task VoiceNote_NoTempFile_NoTextInLogs()
    {
        FakeSpeechToText speech = new() { Text = "Gizlidöküm ali bugün ödevini getirmedi", DurationSeconds = 18 };
        FakeAiTextGenerator text = new() { Reply = "Gizlitaslak Sayın Veli, Ali bugün ödevini getirmedi." };
        using OkulApiFactory factory = Factory(this.database, speech, text);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid conversation = await StartedAsync(client, w.Class.Teacher, w.Ali, w.AliMother.UserId);

        ConcurrentQueue<string> created = new();
        using (FileSystemWatcher watcher = new(Path.GetTempPath(), "ASPNETCORE_*"))
        {
            watcher.Created += (_, e) => created.Enqueue(e.Name ?? string.Empty);
            watcher.EnableRaisingEvents = true;
            var (status, body, raw) = await VoiceNoteAsync(client, w.Class.Teacher, conversation, Audio(1024 * 1024));
            status.ShouldBe(HttpStatusCode.OK, raw);
            await Task.Delay(300, TestContext.Current.CancellationToken);
            speech.LastLength.ShouldBe(1024 * 1024);

            Guid draftId = body.GetProperty("draftId").GetGuid();
            (await SendDraftAsync(client, w.Class.Teacher, conversation, "Gizlimesaj Sayın Veli, Ali yarın ödevini getirsin.", draftId)).Status.ShouldBe(HttpStatusCode.OK);
        }

        created.ShouldBeEmpty("ses geçici dosyaya yazılmaz");
        string log = factory.ReadLog();
        foreach (string secret in new[] { "Gizlidöküm", "Gizlitaslak", "Gizlimesaj", "Ali Yılmaz", "Veli Kişi" })
        {
            log.ShouldNotContain(secret, customMessage: secret);
        }

        string drafted = log.Split('\n').Where(l => l.Contains("Sesli not taslağı oluşturuldu", StringComparison.Ordinal)).ShouldHaveSingleItem();
        drafted.ShouldContain(conversation.ToString());
        drafted.ShouldContain("ses 18 sn");
        log.ShouldContain("AI taslağı onaylandı");
    }
}
