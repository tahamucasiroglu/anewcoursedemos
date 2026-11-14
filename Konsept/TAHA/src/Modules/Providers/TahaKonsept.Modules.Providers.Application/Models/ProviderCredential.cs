namespace TahaKonsept.Modules.Providers.Application.Models;

/// <summary>
/// Bir platformun çözülmüş (düz metin) kimlik bilgisi. Yalnızca bellekte, çağrı süresince yaşar; loglanmaz
/// (<see cref="ToString"/> anahtarı gizler).
/// </summary>
/// <param name="Platform">Platform kimliği.</param>
/// <param name="ApiKey">Düz metin anahtar.</param>
/// <param name="Region">Bölge (Azure Speech).</param>
public sealed record ProviderCredential(string Platform, string ApiKey, string? Region)
{
    /// <summary>Anahtarı içermeyen metin gösterimi.</summary>
    /// <returns>Platform ve bölge.</returns>
    public override string ToString() => $"ProviderCredential {{ Platform = {Platform}, Region = {Region} }}";
}
