using System.Net;
using System.Net.Mime;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Ai;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Ai.AiTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration;

/// <summary>
/// Beklenen hata yanıtları (doğrulama 400, bulunamadı 404) JSON içerik türüyle döner (spec 0012 review round 2, Düşük-1): hata
/// ara katmanı istek başına işleyiciye geçince (T19) nArchitecture temel sınıfının ayarladığı içerik türü düşmüştü; istemciler
/// gövdeyi içerik türüne bakarak ayrıştırır.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ErrorResponseContentTypeTests
{
    private readonly TestDatabase database;

    public ErrorResponseContentTypeTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task ExpectedErrors_ValidationAndNotFound_CarryJsonContentType()
    {
        using OkulApiFactory factory = Factory(this.database, new FakeSpeechToText(), new FakeAiTextGenerator());
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid conversation = await StartedAsync(client, w.Class.Teacher, w.Ali, w.AliMother.UserId);

        using HttpResponseMessage invalid = await SendAuthorizedAsync(client, HttpMethod.Post, $"/api/conversations/{conversation}/messages", w.Class.Teacher, new { body = "   " });
        using HttpResponseMessage missing = await SendAuthorizedAsync(client, HttpMethod.Delete, $"/api/ai/drafts/{Guid.NewGuid()}", w.Class.Teacher);

        invalid.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        invalid.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(MediaTypeNames.Application.Json);
        Messages(await BodyAsync(invalid)).ShouldContain("Mesaj boş olamaz");
        missing.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        missing.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(MediaTypeNames.Application.Json);
        Detail(await BodyAsync(missing)).ShouldBe("Taslak bulunamadı");
    }

    private static async Task<JsonElement> BodyAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement.Clone();
}
