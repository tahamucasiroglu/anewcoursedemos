using System.Net;
using System.Net.Http.Headers;
using System.Text;
using TahaKonsept.Modules.Providers.Tests.Support;

namespace TahaKonsept.Modules.Providers.Tests.Media;

/// <summary>
/// Görsel/ses testleri için sahte HTTP işleyicisi: sıradaki hazır yanıtı (JSON ya da ham bayt) döner, gelen istekleri gövde ve
/// içerik başlıklarıyla kaydeder. Ağ çağrısı yapılmaz.
/// </summary>
public sealed class MediaFakeHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpResponseMessage>> _responses = new();

    /// <summary>Kaydedilen istekler.</summary>
    public List<CapturedRequest> Requests { get; } = [];

    /// <summary>Kaydedilen isteklerin içerik türleri (sırayla).</summary>
    public List<string?> ContentTypes { get; } = [];

    /// <summary>JSON yanıtı sıraya ekler.</summary>
    /// <param name="json">Gövde.</param>
    /// <param name="status">Durum kodu.</param>
    public MediaFakeHandler Json(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        _responses.Enqueue(() => new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        return this;
    }

    /// <summary>Ham bayt yanıtı sıraya ekler.</summary>
    /// <param name="bytes">Gövde.</param>
    /// <param name="contentType">İçerik türü.</param>
    public MediaFakeHandler Bytes(byte[] bytes, string contentType)
    {
        _responses.Enqueue(() =>
        {
            var content = new ByteArrayContent(bytes);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        });
        return this;
    }

    /// <summary>Sıradaki istekte istisna fırlatır (ağ kopması benzetimi).</summary>
    /// <param name="exception">İstisna.</param>
    public MediaFakeHandler Throw(Exception exception)
    {
        _responses.Enqueue(() => throw exception);
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
        ContentTypes.Add(request.Content?.Headers.ContentType?.MediaType);
        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("Sahte işleyicide yanıt kalmadı: " + request.RequestUri);
        }

        return _responses.Dequeue()();
    }
}
