using System.Net;

namespace Konsept.Web.Tests.Support;

/// <summary>Gönderilen istekleri kaydeden ve sabit bir JSON cevap (varsayılan 200) dönen HTTP işleyicisi.</summary>
/// <param name="responseJson">Dönülecek gövde.</param>
/// <param name="status">Dönülecek durum kodu.</param>
public sealed class RecordingHandler(string responseJson, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
{
    /// <summary>Gets kaydedilen istekler.</summary>
    public List<HttpRequestMessage> Requests { get; } = [];

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json"),
        });
    }
}
