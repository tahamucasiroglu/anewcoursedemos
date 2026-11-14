using System.Net;
using System.Text;

namespace TahaKonsept.Modules.Providers.Tests.Support;

/// <summary>
/// Sahte HTTP işleyicisi: sıradaki hazır yanıtı döner ve gelen istekleri (gövdesiyle) kaydeder. Ağ çağrısı yapılmaz.
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpResponseMessage>> _responses = new();

    /// <summary>Kaydedilen istekler.</summary>
    public List<CapturedRequest> Requests { get; } = [];

    /// <summary>JSON yanıtı sıraya ekler.</summary>
    /// <param name="json">Gövde.</param>
    /// <param name="status">Durum kodu.</param>
    public FakeHttpMessageHandler EnqueueJson(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        _responses.Enqueue(() => new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        return this;
    }

    /// <summary>SSE yanıtı sıraya ekler.</summary>
    /// <param name="events">Olay satırları (<c>event:</c>/<c>data:</c>), boş satırlar dahil.</param>
    public FakeHttpMessageHandler EnqueueSse(string events)
    {
        _responses.Enqueue(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(events, Encoding.UTF8, "text/event-stream") });
        return this;
    }

    /// <summary>Verilen SSE başlangıcından sonra gövdesi kopan yanıtı sıraya ekler.</summary>
    /// <param name="prefix">Kopmadan önceki olay satırları.</param>
    public FakeHttpMessageHandler EnqueueBrokenSse(string prefix)
    {
        _responses.Enqueue(() =>
        {
            var content = new StreamContent(new BrokenAfterStream(Encoding.UTF8.GetBytes(prefix)));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/event-stream");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        });
        return this;
    }

    /// <summary>İstemci oluşturur.</summary>
    public HttpClient CreateClient() => new(this, disposeHandler: false);

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        var headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase);
        Requests.Add(new CapturedRequest(request.Method, request.RequestUri!, headers, body));
        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("Sahte işleyicide yanıt kalmadı: " + request.RequestUri);
        }

        return _responses.Dequeue()();
    }
}
