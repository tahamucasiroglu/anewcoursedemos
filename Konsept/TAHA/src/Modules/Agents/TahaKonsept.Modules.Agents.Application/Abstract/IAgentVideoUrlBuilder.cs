namespace TahaKonsept.Modules.Agents.Application.Abstract;

/// <summary>
/// Ajanın yerel HTTP sunucusu (MIMARI §5, varsayılan port 7120) için adres üretici:
/// <see cref="BuildVideoUrl"/> → <c>http://{lan}:{port}/videos/{id}?k={key}</c>, <see cref="BuildThumbnailUrl"/> → <c>/thumbs/{id}</c>,
/// <see cref="BuildFileUrl"/> → herhangi bir göreli yol. IPv6 adresleri köşeli paranteze alınır, anahtar URL-kodlanır.
/// </summary>
public interface IAgentVideoUrlBuilder
{
    /// <summary>Video adresi.</summary>
    /// <param name="lanAddress">LAN adresi.</param>
    /// <param name="port">Port.</param>
    /// <param name="streamKey">Akış anahtarı.</param>
    /// <param name="videoId">Video.</param>
    /// <returns>Mutlak adres.</returns>
    string BuildVideoUrl(string lanAddress, int port, string streamKey, Guid videoId);

    /// <summary>Küçük resim adresi.</summary>
    /// <param name="lanAddress">LAN adresi.</param>
    /// <param name="port">Port.</param>
    /// <param name="streamKey">Akış anahtarı.</param>
    /// <param name="videoId">Video.</param>
    /// <returns>Mutlak adres.</returns>
    string BuildThumbnailUrl(string lanAddress, int port, string streamKey, Guid videoId);

    /// <summary>Göreli yol için adres.</summary>
    /// <param name="lanAddress">LAN adresi.</param>
    /// <param name="port">Port.</param>
    /// <param name="streamKey">Akış anahtarı.</param>
    /// <param name="relativePath">Göreli yol (ör. <c>/previews/x.wav</c>).</param>
    /// <returns>Mutlak adres.</returns>
    string BuildFileUrl(string lanAddress, int port, string streamKey, string relativePath);
}
