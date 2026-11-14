using TahaKonsept.Modules.Production.Application.Abstract;

namespace TahaKonsept.Modules.Production.Infrastructure.Guides.Base;

/// <summary>
/// <see cref="IAuthoringGuideProvider"/> ortak davranışı: motor anahtarını kırpıp küçük harfe çevirir, boşsa <c>null</c> döner;
/// asıl kılavuz metni somut sınıfın <see cref="Load"/> uygulamasından gelir.
/// </summary>
public abstract class AuthoringGuideProviderBase : IAuthoringGuideProvider
{
    /// <summary>Motorun kılavuzu.</summary>
    /// <param name="engineKey">Motor anahtarı.</param>
    /// <returns>Kılavuz ya da <c>null</c>.</returns>
    public string? GetGuide(string engineKey) =>
        string.IsNullOrWhiteSpace(engineKey) ? null : Load(engineKey.Trim().ToLowerInvariant());

    /// <summary>Kılavuzu yükler.</summary>
    /// <param name="engineKey">Motor anahtarı (küçük harf).</param>
    /// <returns>Kılavuz ya da <c>null</c>.</returns>
    protected abstract string? Load(string engineKey);
}
