using System.Globalization;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Models;
using TahaKonsept.Modules.Concepts.Domain.Constants;
using TahaKonsept.Modules.Concepts.Domain.Errors;
using TahaKonsept.Modules.Concepts.Domain.ValueObjects;
using TahaKonsept.Modules.Profiles.Application.Abstract;
using TahaKonsept.Shared.Contracts.Profiles;

namespace TahaKonsept.Modules.Concepts.Application.Base;

/// <summary>
/// <see cref="IConceptContextProvider"/> ortak davranışı: ayarlar (yoksa varsayılan) + profil (verilen, bulunamazsa aktif) →
/// bağlam. Etkin ton/izleyici/anlatım = ayar ?? profil varsayılanı. Gerçek bilgi = ayar ?? <see cref="IsFactHeavy"/>
/// (profil şablon anahtarı / adı / içeriğinde <see cref="FactKeywords"/> geçiyor mu). Anlatım hızı = <see cref="Pace"/>
/// (kullanıcının üretimde seçtiği ses modeli; ölçülmüş hız varsa o) ?? dil sabiti (<see cref="ConceptLimits"/>).
/// </summary>
public abstract class ConceptContextProviderBase : IConceptContextProvider
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    private static readonly string[] DefaultFactKeywords =
    [
        "tarih", "spor", "bilim", "antik", "gizem", "futbol", "basketbol", "olimpiyat", "bizans", "osmanl", "selçuk", "roma",
        "savaş", "imparator", "biyografi", "belgesel", "uzay", "keşif", "arkeoloji", "history", "sport", "science",
    ];

    /// <summary>Oluşturur.</summary>
    /// <param name="settings">Ayar deposu.</param>
    /// <param name="profiles">Profil okuyucu.</param>
    /// <param name="promptBuilder">Profil prompt kurucu.</param>
    /// <param name="pace">Ses modeline göre anlatım hızı portu.</param>
    protected ConceptContextProviderBase(IConceptSettingsRepository settings, IProfileReader profiles, IProfilePromptBuilder promptBuilder, INarrationPaceSource pace)
    {
        Pace = pace ?? throw new ArgumentNullException(nameof(pace));
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        Profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        PromptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
    }

    /// <summary>Ayar deposu.</summary>
    protected IConceptSettingsRepository Settings { get; }

    /// <summary>Profil okuyucu.</summary>
    protected IProfileReader Profiles { get; }

    /// <summary>Profil prompt kurucu.</summary>
    protected IProfilePromptBuilder PromptBuilder { get; }

    /// <summary>Anlatım hızı portu.</summary>
    protected INarrationPaceSource Pace { get; }

    /// <summary>Gerçek bilgi gerektiren alan anahtar kelimeleri (küçük harf).</summary>
    protected virtual IReadOnlyList<string> FactKeywords => DefaultFactKeywords;

    /// <summary>Bağlamı kurar.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Bağlam.</returns>
    public virtual async Task<Result<ConceptGenerationContext>> ResolveAsync(Guid userId, Guid? profileId, CancellationToken cancellationToken = default)
    {
        var preferences = (await Settings.GetForUserAsync(userId, cancellationToken))?.ToPreferences() ?? ConceptPreferences.Default;
        ProfileDto? profile = null;
        if (profileId is { } pid)
        {
            profile = await Profiles.GetAsync(pid, cancellationToken);
        }

        profile ??= await Profiles.GetActiveAsync(userId, cancellationToken);
        if (profile is null)
        {
            return ConceptErrors.NoProfile;
        }

        var language = string.IsNullOrWhiteSpace(profile.Language) ? "tr" : profile.Language.Trim().ToLowerInvariant();
        var wordsPerSecond = await Pace.GetWordsPerSecondAsync(userId, language, cancellationToken) ?? FallbackWordsPerSecond(language);
        return new ConceptGenerationContext
        {
            UserId = userId,
            ProfileId = profile.Id,
            ProfileBlock = PromptBuilder.Build(profile),
            Preferences = preferences,
            Language = language,
            Format = profile.Format,
            TargetDurationSec = profile.TargetDurationSec > 0 ? profile.TargetDurationSec : 60,
            Tone = preferences.Tone ?? profile.DefaultTone ?? string.Empty,
            Audience = preferences.Audience ?? profile.DefaultAudience ?? string.Empty,
            NarrativeStyle = preferences.NarrativeStyle ?? profile.DefaultNarrativeStyle ?? string.Empty,
            FactGrounding = preferences.FactGrounding ?? IsFactHeavy(profile),
            WordsPerSecond = wordsPerSecond,
        };
    }

    /// <summary>Port bilgi vermezse dil sabiti.</summary>
    /// <param name="language">Dil.</param>
    /// <returns>Kelime/sn.</returns>
    public static double FallbackWordsPerSecond(string language) =>
        language.StartsWith("tr", StringComparison.Ordinal) ? ConceptLimits.TurkishWordsPerSecond : ConceptLimits.DefaultWordsPerSecond;

    /// <summary>Profil gerçek bilgi gerektiren bir alanda mı.</summary>
    /// <param name="profile">Profil.</param>
    /// <returns>Gerektiriyor mu.</returns>
    public virtual bool IsFactHeavy(ProfileDto? profile)
    {
        if (profile is null)
        {
            return false;
        }

        var text = $"{profile.TemplateKey} {profile.Name} {profile.ChannelDescription}".ToLower(Tr);
        return FactKeywords.Any(k => text.Contains(k, StringComparison.Ordinal));
    }
}
