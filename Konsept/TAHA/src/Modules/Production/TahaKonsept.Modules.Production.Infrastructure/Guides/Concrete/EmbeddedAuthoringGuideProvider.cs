using TahaKonsept.Agent.Engines.Video.Authoring;
using TahaKonsept.Modules.Production.Infrastructure.Guides.Base;

namespace TahaKonsept.Modules.Production.Infrastructure.Guides.Concrete;

/// <summary>
/// Video motoru projesine gömülü yazım kılavuzlarını (<see cref="AuthoringGuides.ForEngine"/>) sağlayan uygulama (singleton).
/// Ajan render'ında kullanılan kılavuzla sunucunun LLM'e gönderdiği kılavuz böylece hep aynı sürümdür.
/// </summary>
public sealed class EmbeddedAuthoringGuideProvider : AuthoringGuideProviderBase
{
    /// <summary>Kılavuzu yükler.</summary>
    /// <param name="engineKey">Motor anahtarı.</param>
    /// <returns>Kılavuz ya da <c>null</c>.</returns>
    protected override string? Load(string engineKey) => AuthoringGuides.ForEngine(engineKey);
}
