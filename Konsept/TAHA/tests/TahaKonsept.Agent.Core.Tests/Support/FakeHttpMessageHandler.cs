using System.Net;
using System.Net.Http.Json;
using TahaKonsept.Shared.Contracts.Common;

namespace TahaKonsept.Agent.Core.Tests.Support;

/// <summary>
/// Sahte HTTP işleyici: her isteği (istek, sıra) → yanıt fonksiyonuna verir ve istek yollarını kaydeder.
/// Yardımcılar: <see cref="Json"/>, <see cref="Problem"/>.
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, int, HttpResponseMessage> _responder;
    private readonly object _gate = new();
    private int _calls;

    /// <summary>Oluşturur.</summary>
    /// <param name="responder">Yanıt üretici.</param>
    public FakeHttpMessageHandler(Func<HttpRequestMessage, int, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    /// <summary>Gelen isteklerin mutlak yolları (sırayla).</summary>
    public List<string> Requests { get; } = [];

    /// <summary>JSON yanıt üretir.</summary>
    /// <param name="status">Durum kodu.</param>
    /// <param name="body">Gövde.</param>
    /// <returns>Yanıt.</returns>
    public static HttpResponseMessage Json(HttpStatusCode status, object body) =>
        new(status) { Content = JsonContent.Create(body, body.GetType(), options: ContractJson.Options) };

    /// <summary>ProblemDetails yanıtı üretir (<c>code</c> alanıyla).</summary>
    /// <param name="status">Durum kodu.</param>
    /// <param name="code">Hata kodu.</param>
    /// <returns>Yanıt.</returns>
    public static HttpResponseMessage Problem(HttpStatusCode status, string code) =>
        Json(status, new Dictionary<string, object> { ["title"] = "Hata", ["status"] = (int)status, ["code"] = code, ["detail"] = code });

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        int index;
        lock (_gate)
        {
            Requests.Add(request.Method + " " + request.RequestUri!.AbsolutePath);
            index = _calls++;
        }

        return Task.FromResult(_responder(request, index));
    }
}
