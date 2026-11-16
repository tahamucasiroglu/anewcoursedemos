namespace Konsept.Api.Tests.Fakes;

/// <summary>
/// Sahte sağlayıcı HTTP işleyicisi (plan 0013): her isteği ve gövdesini (istek atılmadan önce okunur) kaydeder, verilen yanıtı
/// döndürür. Gerçek ağ çağrısı yoktur.
/// </summary>
/// <param name="respond">Yanıt üretici.</param>
public sealed class RecordingProviderHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    /// <summary>Gets istekler.</summary>
    public List<HttpRequestMessage> Requests { get; } = [];

    /// <summary>Gets istek gövdeleri.</summary>
    public List<string> Bodies { get; } = [];

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        Bodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
        return respond(request);
    }
}
