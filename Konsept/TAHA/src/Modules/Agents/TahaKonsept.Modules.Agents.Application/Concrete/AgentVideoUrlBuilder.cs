using TahaKonsept.Modules.Agents.Application.Base;

namespace TahaKonsept.Modules.Agents.Application.Concrete;

/// <summary>
/// MIMARI §5'teki ajan yerel sunucusu düzeni: <c>http://{lan}:{port}/videos/{id}?k=</c>, <c>/thumbs/{id}?k=</c>.
/// </summary>
public sealed class AgentVideoUrlBuilder : AgentVideoUrlBuilderBase
{
    /// <summary>Şema.</summary>
    protected override string Scheme => "http";

    /// <summary>Video yolu.</summary>
    protected override string VideosSegment => "videos";

    /// <summary>Küçük resim yolu.</summary>
    protected override string ThumbnailsSegment => "thumbs";
}
