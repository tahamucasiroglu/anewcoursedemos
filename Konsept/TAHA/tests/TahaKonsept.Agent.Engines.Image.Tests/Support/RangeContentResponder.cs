using System.Net;
using System.Net.Http.Headers;

namespace TahaKonsept.Agent.Engines.Image.Tests.Support;

/// <summary>
/// Bir bayt dizisini HTTP Range destekleyerek sunan sahte sunucu mantığı. İlk <see cref="BreakFirstAfter"/> istekte
/// verilen bayttan sonra bağlantıyı koparır (devam testleri için).
/// </summary>
public sealed class RangeContentResponder
{
    private readonly byte[] _content;
    private int _requestCount;

    /// <summary>Sunucuyu oluşturur.</summary>
    /// <param name="content">Sunulan içerik.</param>
    public RangeContentResponder(byte[] content)
    {
        _content = content;
    }

    /// <summary>İlk istekte kaç bayttan sonra kopsun (<c>null</c> = kopmasın).</summary>
    public int? BreakFirstAfter { get; init; }

    /// <summary>Range başlığını yok sayıp hep 200 dönsün mü.</summary>
    public bool IgnoreRange { get; init; }

    /// <summary>İsteği yanıtlar.</summary>
    /// <param name="request">İstek.</param>
    /// <returns>Yanıt.</returns>
    public HttpResponseMessage Respond(HttpRequestMessage request)
    {
        var count = Interlocked.Increment(ref _requestCount);
        var from = IgnoreRange ? 0 : request.Headers.Range?.Ranges.FirstOrDefault()?.From ?? 0;
        if (from >= _content.Length && from > 0)
        {
            return new HttpResponseMessage(HttpStatusCode.RequestedRangeNotSatisfiable);
        }

        var slice = _content[(int)from..];
        Stream body = count == 1 && BreakFirstAfter is { } breakAfter ? new BrokenStream(slice, breakAfter) : new MemoryStream(slice);
        var response = new HttpResponseMessage(from > 0 ? HttpStatusCode.PartialContent : HttpStatusCode.OK)
        {
            Content = new StreamContent(body),
        };
        response.Content.Headers.ContentLength = slice.Length;
        if (from > 0)
        {
            response.Content.Headers.ContentRange = new ContentRangeHeaderValue(from, _content.Length - 1, _content.Length);
        }

        return response;
    }
}
