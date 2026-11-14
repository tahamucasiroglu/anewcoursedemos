using System.Net;

namespace Konsept.Web.Tests.Support;

/// <summary>Gönderilen istekleri kaydeden ve sabit bir JSON cevap dönen HTTP işleyicisi.</summary>
/// <param name="responseJson">Dönülecek gövde.</param>
public sealed class RecordingHandler(string responseJson) : HttpMessageHandler
{
    /// <summary>Gets kaydedilen istekler.</summary>
    public List<HttpRequestMessage> Requests { get; } = [];

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json"),
        });
    }
}
