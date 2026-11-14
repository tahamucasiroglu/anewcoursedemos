using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TahaKonsept.Modules.Production.Normalization.Abstract;
using TahaKonsept.Modules.Production.Normalization.Concrete;
using TahaKonsept.Modules.Production.Normalization.Text;

namespace TahaKonsept.Modules.Production.Normalization.DependencyInjection;

/// <summary>
/// Normalizasyon DI kaydı (<see cref="AddSpeechTextNormalization"/>): tümü tekil (durumsuz, iş parçacığı güvenli).
/// <see cref="ISpeechTextNormalizer"/> → <see cref="LanguageRoutingSpeechTextNormalizer"/>; ayrıca
/// <see cref="INormalizerSelector"/>, <see cref="INumberToTurkishWords"/>, <see cref="ITurkishSuffixHarmonizer"/>,
/// <see cref="IBuiltInLexiconProvider"/>, <see cref="TurkishSpeechTextNormalizer"/>, <see cref="LexiconOnlySpeechTextNormalizer"/>.
/// </summary>
public static class SpeechTextNormalizationServiceCollectionExtensions
{
    /// <summary>Seslendirme metni normalizasyon servislerini kaydeder (tekrar çağrılabilir; TryAdd).</summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <returns>Aynı koleksiyon.</returns>
    public static IServiceCollection AddSpeechTextNormalization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton(NumberToTurkishWords.Default);
        services.TryAddSingleton<ITurkishSuffixHarmonizer, TurkishSuffixHarmonizer>();
        services.TryAddSingleton<IBuiltInLexiconProvider, EmbeddedBuiltInLexiconProvider>();
        services.TryAddSingleton<TurkishSpeechTextNormalizer>();
        services.TryAddSingleton<LexiconOnlySpeechTextNormalizer>();
        services.TryAddSingleton<INormalizerSelector, LanguageNormalizerSelector>();
        services.TryAddSingleton<ISpeechTextNormalizer, LanguageRoutingSpeechTextNormalizer>();
        return services;
    }
}
