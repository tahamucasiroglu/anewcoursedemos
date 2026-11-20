using System.Net;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Communication;

/// <summary>
/// Spec 0011 AC-11 (gizlilik): duyuru başlığı/metni, onay sorusu, mesaj metni ve kişi adları log satırlarına ve sahte
/// bildirim göndericisinin log satırlarına yazılmaz — bildirim satırında yalnızca tür ve kimlikler bulunur.
/// </summary>
[Trait("Category", "Integration")]
public sealed class CommunicationSecretHygieneTests
{
    private readonly TestDatabase database;

    public CommunicationSecretHygieneTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Logs_HoldOnlyIds_NoTextOrNames()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid ozel = await StudentAsync(client, w.Class, "Gizliadlı Öğrenci");
        ParentAccount ozelMother = await ParentAsync(client, w.Class, ozel);

        Guid form = await PublishedAsync(client, w.Class.Teacher, Form("Student", ozel, "2026-11-27", question: "Gizlisoru izin?", title: "Gizlibaşlık", body: "Gizlimetin içerik"));
        (await DetailAsync(client, ozelMother.Token, form)).Status.ShouldBe(HttpStatusCode.OK);
        (await AnswerAsync(client, ozelMother.Token, form, ozel, "Approved")).Status.ShouldBe(HttpStatusCode.OK);
        (await ResultsAsync(client, w.Class.Teacher, form)).Status.ShouldBe(HttpStatusCode.OK);
        Guid conversation = await StartedAsync(client, ozelMother.Token, ozel, w.Class.TeacherId);
        (await SendAsync(client, ozelMother.Token, conversation, "Gizlimesaj sağlık durumu")).Status.ShouldBe(HttpStatusCode.OK);
        (await SendAsync(client, w.Class.Teacher, conversation, "Gizlicevap")).Status.ShouldBe(HttpStatusCode.OK);
        (await MessagesAsync(client, w.Class.Teacher, conversation)).Status.ShouldBe(HttpStatusCode.OK);

        string log = factory.ReadLog();
        foreach (string secret in new[] { "Gizlibaşlık", "Gizlimetin", "Gizlisoru", "Gizlimesaj", "Gizlicevap", "Gizliadlı", "Veli Kişi", ozelMother.Email })
        {
            log.ShouldNotContain(secret, customMessage: secret);
        }

        string[] notifications = [.. log.Split('\n').Where(l => l.Contains("Bildirim ", StringComparison.Ordinal) && l.Contains(" → kişi ", StringComparison.Ordinal))];
        notifications.ShouldContain(l => l.Contains("ConsentForm", StringComparison.Ordinal) && l.Contains(ozelMother.UserId.ToString(), StringComparison.Ordinal) && l.Contains(form.ToString(), StringComparison.Ordinal));
        notifications.ShouldContain(l => l.Contains("Message", StringComparison.Ordinal) && l.Contains(w.Class.TeacherId.ToString(), StringComparison.Ordinal));
        log.ShouldContain("Duyuru yayınlandı");
        log.ShouldContain("Mesaj gönderildi");
    }
}
