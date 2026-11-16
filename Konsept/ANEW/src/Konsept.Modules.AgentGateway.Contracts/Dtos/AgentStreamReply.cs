namespace Konsept.Modules.AgentGateway.Contracts.Dtos;

/// <summary>Yerel akış cevabı: yalnızca bu bilgisayardan açılan kısa ömürlü adres ya da hata (<see cref="NotFound"/>).</summary>
/// <param name="Url">Adres (belirteç içerir; loglanmaz) ya da <c>null</c>.</param>
/// <param name="Error">Hata kodu ya da <c>null</c>.</param>
public sealed record AgentStreamReply(string? Url, string? Error)
{
    /// <summary>Video dosyası ajan klasöründe yok.</summary>
    public const string NotFound = "not_found";
}
