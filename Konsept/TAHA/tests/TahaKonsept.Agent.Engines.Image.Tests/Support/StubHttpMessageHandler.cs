namespace TahaKonsept.Agent.Engines.Image.Tests.Support;

/// <summary>Her isteği verilen fonksiyonla yanıtlayan sahte HTTP işleyicisi; gelen istekleri kaydeder.</summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

    /// <summary>İşleyiciyi oluşturur.</summary>
    /// <param name="respond">Yanıt üretici.</param>
    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        _respond = respond;
    }

    /// <summary>Alınan isteklerin adresleri ve Range başlangıçları.</summary>
    public List<(Uri Uri, long? RangeFrom)> Requests { get; } = [];

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        lock (Requests)
        {
            Requests.Add((request.RequestUri!, request.Headers.Range?.Ranges.FirstOrDefault()?.From));
        }

        return Task.FromResult(_respond(request));
    }
}
