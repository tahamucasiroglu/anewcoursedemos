using System.Globalization;
using TahaKonsept.Modules.Agents.Application.Abstract;

namespace TahaKonsept.Modules.Agents.Application.Base;

/// <summary>
/// Ajan yerel sunucu adres üretici tabanı: <see cref="BuildFileUrl"/> ortak biçimi kurar (IPv6 köşeli parantez, tek <c>/</c>,
/// <c>k</c> sorgu parametresi URL-kodlu). Türetilen sınıf şemayı (<see cref="Scheme"/>) ve yol öneklerini verir.
/// </summary>
public abstract class AgentVideoUrlBuilderBase : IAgentVideoUrlBuilder
{
    /// <summary>Akış anahtarı sorgu parametresi.</summary>
    public const string StreamKeyQueryName = "k";

    /// <summary>URL şeması (ör. <c>http</c>).</summary>
    protected abstract string Scheme { get; }

    /// <summary>Video yolu öneki (ör. <c>videos</c>).</summary>
    protected abstract string VideosSegment { get; }

    /// <summary>Küçük resim yolu öneki (ör. <c>thumbs</c>).</summary>
    protected abstract string ThumbnailsSegment { get; }

    /// <summary>Video adresi.</summary>
    /// <param name="lanAddress">LAN adresi.</param>
    /// <param name="port">Port.</param>
    /// <param name="streamKey">Akış anahtarı.</param>
    /// <param name="videoId">Video.</param>
    /// <returns>Adres.</returns>
    public string BuildVideoUrl(string lanAddress, int port, string streamKey, Guid videoId) =>
        BuildFileUrl(lanAddress, port, streamKey, VideosSegment + "/" + videoId.ToString("D", CultureInfo.InvariantCulture));

    /// <summary>Küçük resim adresi.</summary>
    /// <param name="lanAddress">LAN adresi.</param>
    /// <param name="port">Port.</param>
    /// <param name="streamKey">Akış anahtarı.</param>
    /// <param name="videoId">Video.</param>
    /// <returns>Adres.</returns>
    public string BuildThumbnailUrl(string lanAddress, int port, string streamKey, Guid videoId) =>
        BuildFileUrl(lanAddress, port, streamKey, ThumbnailsSegment + "/" + videoId.ToString("D", CultureInfo.InvariantCulture));

    /// <summary>Göreli yol için adres.</summary>
    /// <param name="lanAddress">LAN adresi.</param>
    /// <param name="port">Port.</param>
    /// <param name="streamKey">Akış anahtarı.</param>
    /// <param name="relativePath">Göreli yol.</param>
    /// <returns>Adres.</returns>
    public string BuildFileUrl(string lanAddress, int port, string streamKey, string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lanAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(streamKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(port, 65535);

        var host = lanAddress.Trim();
        if (host.Contains(':', StringComparison.Ordinal) && !host.StartsWith('['))
        {
            host = "[" + host + "]";
        }

        var path = "/" + (relativePath ?? string.Empty).Trim().TrimStart('/');
        var separator = path.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{Scheme}://{host}:{port}{path}{separator}{StreamKeyQueryName}={Uri.EscapeDataString(streamKey)}");
    }
}
