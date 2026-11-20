using System.Net;
using System.Text;

namespace OkulYonetim.Tests.Unit.Ai;

/// <summary>
/// Sahte HTTP işleyici (spec 0012 AC-9 birim testleri — servis gerekmeden): gelen isteğin yolunu, içerik türünü ve gövdesini
/// saklar; verilen yanıtı döndürür, isterse bekler (zaman aşımı) ya da bağlantı hatası fırlatır. Kaç kez çağrıldığını sayar.
/// </summary>
public sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond;

    public FakeHttpHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
    {
        this.respond = respond;
    }

    public int Calls { get; private set; }

    public string? Path { get; private set; }

    public string? ContentType { get; private set; }

    public byte[] Body { get; private set; } = [];

    public string BodyText => Encoding.UTF8.GetString(this.Body);

    public static FakeHttpHandler Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new((_, _) => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") }));

    public static FakeHttpHandler Hanging() =>
        new(async (_, token) =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

    public static FakeHttpHandler Refused() =>
        new((_, _) => throw new HttpRequestException("bağlantı reddedildi"));

    public HttpClient Client(string baseUrl, TimeSpan? timeout = null) =>
        new(this) { BaseAddress = new Uri(baseUrl), Timeout = timeout ?? TimeSpan.FromSeconds(10) };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        this.Calls++;
        this.Path = request.RequestUri?.AbsolutePath;
        this.ContentType = request.Content?.Headers.ContentType?.ToString();
        this.Body = request.Content is null ? [] : await request.Content.ReadAsByteArrayAsync(cancellationToken);
        return await this.respond(request, cancellationToken);
    }
}
