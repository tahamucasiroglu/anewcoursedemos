namespace Konsept.Api.Tests.Fakes;

/// <summary>Sahte sağlayıcı: istekleri kaydeder, cevabı verilen işlevden üretir (testte dışarıya hiç istek gitmez).</summary>
/// <param name="respond">Cevap üreticisi (iptal belirteciyle).</param>
public sealed class StubProviderHandler(Func<CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
{
    /// <summary>Gets kaydedilen istekler.</summary>
    public List<HttpRequestMessage> Requests { get; } = [];

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return respond(cancellationToken);
    }
}
