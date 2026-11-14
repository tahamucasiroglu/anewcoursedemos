using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Domain.Entities;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Shared.Contracts.Production;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="ISpeechRateService"/> ortak davranışı. Taban hız (1.0×) sırası: ölçülmüş ortalama (en az <see cref="MinCalibratedWeight"/> örnek)
/// → katalog <c>WordsPerSecond</c> (yalnız Türkçe) → <see cref="LocalTurkishDefault"/> / <see cref="OnlineTurkishDefault"/> (Türkçe) → <c>null</c>.
/// Etkin hız = taban × <c>SpeechSpeed</c>. Ölçüm: kelime / (süre × hız) (hıza indirgenmiş); <see cref="MinSampleWords"/> kelime ya da
/// <see cref="MinSampleSeconds"/> sn altı veya makul aralık (<see cref="MinRate"/>–<see cref="MaxRate"/>) dışı ölçümler atılır.
/// </summary>
public abstract class SpeechRateServiceBase : ISpeechRateService
{
    /// <summary>Yerel Türkçe ses varsayılanı (katalogda hız yoksa).</summary>
    public const double LocalTurkishDefault = 2.0;

    /// <summary>Online Türkçe ses varsayılanı (katalogda hız yoksa).</summary>
    public const double OnlineTurkishDefault = 2.2;

    /// <summary>Ölçülmüş ortalamanın kullanılması için asgari örnek ağırlığı.</summary>
    public const double MinCalibratedWeight = 1;

    /// <summary>Ölçüm için asgari kelime.</summary>
    public const int MinSampleWords = 8;

    /// <summary>Ölçüm için asgari süre (sn).</summary>
    public const double MinSampleSeconds = 3;

    /// <summary>Makul en düşük hız (kelime/sn).</summary>
    public const double MinRate = 0.8;

    /// <summary>Makul en yüksek hız (kelime/sn).</summary>
    public const double MaxRate = 4.5;

    private readonly ISpeechRateStatRepository _stats;
    private readonly IProductionSettingsService _settings;
    private readonly IModelCatalog _catalog;

    /// <summary>Oluşturur.</summary>
    /// <param name="stats">Ölçüm deposu.</param>
    /// <param name="settings">Üretim ayarları servisi.</param>
    /// <param name="catalog">Model kataloğu.</param>
    protected SpeechRateServiceBase(ISpeechRateStatRepository stats, IProductionSettingsService settings, IModelCatalog catalog)
    {
        _stats = stats ?? throw new ArgumentNullException(nameof(stats));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    /// <summary>Etkin hız.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="settings">Ayarlar.</param>
    /// <param name="language">Dil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kelime/sn ya da <c>null</c>.</returns>
    public async Task<double?> GetWordsPerSecondAsync(Guid userId, ProductionSettingsDto settings, string language, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.SpeechMode == MediaMode.Off || string.IsNullOrWhiteSpace(settings.SpeechModelKey))
        {
            return null;
        }

        var lang = NormalizeLanguage(language);
        var key = settings.SpeechModelKey.Trim();
        var stat = await _stats.FindAsync(userId, key, VoiceKeyOf(settings), lang, cancellationToken);
        var baseRate = stat is not null && stat.SampleWeight >= MinCalibratedWeight && stat.WordsPerSecond > 0
            ? stat.WordsPerSecond
            : CatalogRate(key, lang);
        if (baseRate is not { } rate)
        {
            return null;
        }

        return Math.Round(Math.Clamp(rate * SpeedOf(settings), MinRate, MaxRate), 2);
    }

    /// <summary>Kayıtlı ayarlarla etkin hız.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="language">Dil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kelime/sn ya da <c>null</c>.</returns>
    public async Task<double?> GetForUserAsync(Guid userId, string language, CancellationToken cancellationToken)
    {
        var settings = await _settings.GetForUserAsync(userId, cancellationToken);
        return await GetWordsPerSecondAsync(userId, settings, language, cancellationToken);
    }

    /// <summary>Ölçüm kaydeder.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="settings">Ayarlar.</param>
    /// <param name="language">Dil.</param>
    /// <param name="words">Kelime.</param>
    /// <param name="seconds">Süre (sn).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Yeni ortalama ya da <c>null</c>.</returns>
    public async Task<double?> RecordAsync(Guid userId, ProductionSettingsDto settings, string language, int words, double seconds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.SpeechMode == MediaMode.Off || string.IsNullOrWhiteSpace(settings.SpeechModelKey)
            || words < MinSampleWords || double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < MinSampleSeconds)
        {
            return null;
        }

        var baseSeconds = seconds * SpeedOf(settings);
        var measured = words / baseSeconds;
        if (measured is < MinRate or > MaxRate)
        {
            return null;
        }

        var lang = NormalizeLanguage(language);
        var key = settings.SpeechModelKey.Trim();
        var voice = VoiceKeyOf(settings);
        var stat = await _stats.FindAsync(userId, key, voice, lang, cancellationToken);
        if (stat is null)
        {
            stat = SpeechRateStat.Create(userId, key, voice, lang);
            await _stats.AddAsync(stat, cancellationToken);
        }

        stat.AddSample(words, baseSeconds);
        await _stats.UnitOfWork.SaveChangesAsync(cancellationToken);
        return Math.Round(stat.WordsPerSecond, 3);
    }

    /// <summary>Katalog / varsayılan taban hız (yalnız Türkçe; diğer dillerde <c>null</c>).</summary>
    /// <param name="speechModelKey">Model.</param>
    /// <param name="language">Dil (küçük harf).</param>
    /// <returns>Kelime/sn ya da <c>null</c>.</returns>
    protected virtual double? CatalogRate(string speechModelKey, string language)
    {
        if (!language.StartsWith("tr", StringComparison.Ordinal))
        {
            return null;
        }

        var model = _catalog.Find(speechModelKey);
        if (model?.WordsPerSecond is { } rate && rate > 0)
        {
            return rate;
        }

        return model?.RunsOn == ModelRunsOn.Server ? OnlineTurkishDefault : LocalTurkishDefault;
    }

    /// <summary>Ses anahtarı (boş = varsayılan ses).</summary>
    /// <param name="settings">Ayarlar.</param>
    /// <returns>Anahtar.</returns>
    protected static string VoiceKeyOf(ProductionSettingsDto settings) => settings?.VoiceId?.Trim() ?? string.Empty;

    /// <summary>Geçerli hız çarpanı (0.5–2 dışı → 1).</summary>
    /// <param name="settings">Ayarlar.</param>
    /// <returns>Çarpan.</returns>
    protected static double SpeedOf(ProductionSettingsDto settings) =>
        settings?.SpeechSpeed is >= 0.5 and <= 2.0 ? settings.SpeechSpeed : 1.0;

    private static string NormalizeLanguage(string? language) =>
        string.IsNullOrWhiteSpace(language) ? "tr" : language.Trim().ToLowerInvariant();
}
