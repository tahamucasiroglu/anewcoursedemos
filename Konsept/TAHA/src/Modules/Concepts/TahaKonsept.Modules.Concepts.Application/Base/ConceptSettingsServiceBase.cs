using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Mapping;
using TahaKonsept.Modules.Concepts.Domain.Entities;
using TahaKonsept.Modules.Concepts.Domain.ValueObjects;
using TahaKonsept.Modules.Profiles.Application.Abstract;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Base;

/// <summary>
/// <see cref="IConceptSettingsService"/> ortak davranışı: kayıt yoksa varsayılanlar; kaydetmede yoksa oluşturulur.
/// <c>EffectiveFactGrounding</c> = ayar ?? aktif profilin alanı (<see cref="IConceptContextProvider.IsFactHeavy"/>);
/// <c>ConceptModelCanSearchWeb</c> ve <c>EffectiveWordsPerSecond</c> bağlam kurucudan (yalnız yanıtta).
/// </summary>
public abstract class ConceptSettingsServiceBase : IConceptSettingsService
{
    /// <summary>Oluşturur.</summary>
    /// <param name="repository">Ayar deposu.</param>
    /// <param name="currentUser">Oturumdaki kullanıcı.</param>
    /// <param name="profiles">Profil okuyucu.</param>
    /// <param name="contexts">Bağlam kurucu.</param>
    protected ConceptSettingsServiceBase(IConceptSettingsRepository repository, ICurrentUser currentUser, IProfileReader profiles, IConceptContextProvider contexts)
    {
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        CurrentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        Profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        Contexts = contexts ?? throw new ArgumentNullException(nameof(contexts));
    }

    /// <summary>Ayar deposu.</summary>
    protected IConceptSettingsRepository Repository { get; }

    /// <summary>Oturumdaki kullanıcı.</summary>
    protected ICurrentUser CurrentUser { get; }

    /// <summary>Profil okuyucu.</summary>
    protected IProfileReader Profiles { get; }

    /// <summary>Bağlam kurucu.</summary>
    protected IConceptContextProvider Contexts { get; }

    /// <summary>Ayarları getirir.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ayarlar.</returns>
    public virtual async Task<Result<ConceptSettingsDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        var userId = CurrentUser.GetRequiredUserId();
        var preferences = (await Repository.GetForUserAsync(userId, cancellationToken))?.ToPreferences() ?? ConceptPreferences.Default;
        return await ToDtoAsync(userId, preferences, cancellationToken);
    }

    /// <summary>Ayarları kaydeder.</summary>
    /// <param name="settings">Ayarlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kaydedilen.</returns>
    public virtual async Task<Result<ConceptSettingsDto>> SaveAsync(ConceptSettingsDto settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var userId = CurrentUser.GetRequiredUserId();
        var preferences = settings.ToPreferences();
        var entity = await Repository.GetForUserTrackedAsync(userId, cancellationToken);
        if (entity is null)
        {
            await Repository.AddAsync(ConceptSettings.Create(userId, preferences), cancellationToken);
        }
        else
        {
            entity.Update(preferences);
        }

        await Repository.UnitOfWork.SaveChangesAsync(cancellationToken);
        return await ToDtoAsync(userId, preferences, cancellationToken);
    }

    private async Task<Result<ConceptSettingsDto>> ToDtoAsync(Guid userId, ConceptPreferences preferences, CancellationToken cancellationToken)
    {
        var profile = await Profiles.GetActiveAsync(userId, cancellationToken);
        var effective = preferences.FactGrounding ?? Contexts.IsFactHeavy(profile);
        var language = string.IsNullOrWhiteSpace(profile?.Language) ? "tr" : profile.Language;
        return preferences.ToDto(effective) with
        {
            ConceptModelCanSearchWeb = Contexts.CanSearchWeb(preferences.ConceptModelKey),
            EffectiveWordsPerSecond = await Contexts.WordsPerSecondAsync(userId, language, cancellationToken),
        };
    }
}
