using Konsept.Modules.AiGateway.Contracts.Abstract;
using Konsept.Modules.AiGateway.Contracts.Dtos;
using Konsept.Modules.Profiles.Application.Abstract;
using Konsept.Modules.Profiles.Application.Dtos;
using Konsept.Modules.Profiles.Domain.Entities;
using Konsept.Modules.Profiles.Domain.Rules;
using Konsept.Shared.Kernel.Abstract;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Profiles.Application.Base;

/// <summary>
/// Konseptler sayfası ayarları (spec 0005): profil başına model seçimleri (rol başına model + efor), ince ayar ve kelime denetimi.
/// Profil isteği yapan kullanıcının değilse "Profil bulunamadı" (BR-1, AC-8). Model seçimi sunucuda denetlenir (BR-13, AC-3 —
/// kasıtlı bozma hedefi): model, AiGateway'in kullanıcı için <b>seçilebilir</b> dediği metin modellerinden biri olmalı; efor yalnızca
/// modelin desteklediği düzeylerden biri, verilmezse katalog varsayılanı. Bir bölüm bile geçersizse hiçbir şey değişmez.
/// </summary>
/// <param name="profiles">Profil deposu (sahiplik).</param>
/// <param name="settings">Ayar deposu.</param>
/// <param name="models">Metin modeli dizini (AiGateway.Contracts).</param>
/// <param name="currentUser">İsteği yapan kullanıcı.</param>
/// <param name="time">Saat.</param>
public abstract class ConceptSettingsServiceBase(
    IProfileRepository profiles,
    IConceptSettingsRepository settings,
    ITextModelDirectory models,
    ICurrentUser currentUser,
    TimeProvider time) : IConceptSettingsService
{
    /// <inheritdoc />
    public async Task<Result<ConceptSettingsDto>> GetAsync(Guid profileId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<ConceptSettingsDto>.Failure(ProfilesErrors.SignInRequired);
        }

        if (await profiles.GetAsync(userId, profileId, cancellationToken).ConfigureAwait(false) is null)
        {
            return Result<ConceptSettingsDto>.Failure(ProfilesErrors.NotFound);
        }

        var current = await settings.GetAsync(userId, profileId, cancellationToken).ConfigureAwait(false) ?? Defaults(userId, profileId);
        var catalog = await models.ListAsync(cancellationToken).ConfigureAwait(false);
        return Result<ConceptSettingsDto>.Success(ToDto(current, catalog));
    }

    /// <inheritdoc />
    public async Task<Result<ConceptSettingsDto>> SaveAsync(Guid profileId, SaveConceptSettingsRequest? request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<ConceptSettingsDto>.Failure(ProfilesErrors.SignInRequired);
        }

        if (await profiles.GetAsync(userId, profileId, cancellationToken).ConfigureAwait(false) is null)
        {
            return Result<ConceptSettingsDto>.Failure(ProfilesErrors.NotFound);
        }

        var current = await settings.GetAsync(userId, profileId, cancellationToken).ConfigureAwait(false) ?? Defaults(userId, profileId);
        var catalog = await models.ListAsync(cancellationToken).ConfigureAwait(false);
        var roles = ConceptSettingsRules.Roles.ToDictionary(r => r, r => Read(current, r), StringComparer.Ordinal);

        if (request?.Models is { } requested)
        {
            if (ApplyModels(requested, catalog, roles) is { } invalid)
            {
                return Result<ConceptSettingsDto>.Failure(invalid);
            }
        }

        var fineTune = request?.FineTune;
        if (fineTune is not null
            && (!ConceptSettingsRules.IsOption(ConceptSettingsRules.Tones, fineTune.Tone)
                || !ConceptSettingsRules.IsOption(ConceptSettingsRules.Audiences, fineTune.Audience)
                || !ConceptSettingsRules.IsOption(ConceptSettingsRules.Narrations, fineTune.Narration)))
        {
            return Result<ConceptSettingsDto>.Failure(ProfilesErrors.FineTuneInvalid);
        }

        var next = new ConceptSettings
        {
            ProfileId = profileId,
            UserId = userId,
            ConceptModel = roles[ConceptSettingsRules.RoleConcept].Model,
            ConceptEffort = roles[ConceptSettingsRules.RoleConcept].Effort,
            DedupeModel = roles[ConceptSettingsRules.RoleDedupe].Model,
            DedupeEffort = roles[ConceptSettingsRules.RoleDedupe].Effort,
            TopicModel = roles[ConceptSettingsRules.RoleTopic].Model,
            TopicEffort = roles[ConceptSettingsRules.RoleTopic].Effort,
            Tone = fineTune is null ? current.Tone : fineTune.Tone,
            Audience = fineTune is null ? current.Audience : fineTune.Audience,
            Narration = fineTune is null ? current.Narration : fineTune.Narration,
            WordCheck = request?.WordCheck ?? current.WordCheck,
            UpdatedAt = time.GetUtcNow(),
        };
        await settings.SaveAsync(next, cancellationToken).ConfigureAwait(false);
        return Result<ConceptSettingsDto>.Success(ToDto(next, catalog));
    }

    private static Error? ApplyModels(
        IReadOnlyList<RoleModelRequest> requested,
        IReadOnlyList<TextModelInfo> catalog,
        Dictionary<string, (string? Model, string? Effort)> roles)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in requested)
        {
            if (item?.Role is not { } role || !roles.ContainsKey(role) || !seen.Add(role))
            {
                return ProfilesErrors.RoleInvalid;
            }

            if (string.IsNullOrWhiteSpace(item.ModelId))
            {
                roles[role] = (null, null);
                continue;
            }

            // BR-13 (AC-3): yalnızca seçilebilir metin modeli; arayüz atlanarak gönderilen istek de burada reddedilir.
            var model = catalog.FirstOrDefault(m => m.Selectable && string.Equals(m.Id, item.ModelId, StringComparison.Ordinal));
            if (model is null)
            {
                return ProfilesErrors.ModelNotSelectable;
            }

            if (!ConceptSettingsRules.TryResolveEffort(model.EffortLevels, model.DefaultEffort, item.Effort, out var effort))
            {
                return ProfilesErrors.EffortInvalid;
            }

            roles[role] = (model.Id, effort);
        }

        return null;
    }

    private static ConceptSettings Defaults(Guid userId, Guid profileId) => new() { ProfileId = profileId, UserId = userId };

    private static (string? Model, string? Effort) Read(ConceptSettings s, string role) => role switch
    {
        ConceptSettingsRules.RoleConcept => (s.ConceptModel, s.ConceptEffort),
        ConceptSettingsRules.RoleDedupe => (s.DedupeModel, s.DedupeEffort),
        _ => (s.TopicModel, s.TopicEffort),
    };

    private static ConceptSettingsDto ToDto(ConceptSettings s, IReadOnlyList<TextModelInfo> catalog)
    {
        var names = catalog.ToDictionary(m => m.Id, m => m.Name, StringComparer.Ordinal);
        var roles = ConceptSettingsRules.Roles
            .Select(r =>
            {
                var (model, effort) = Read(s, r);
                return new RoleModelDto(r, model, model is null ? null : names.GetValueOrDefault(model, model), effort);
            })
            .ToList();
        var selectable = catalog.Where(m => m.Selectable)
            .Select(m => new SelectableModelDto(m.Id, m.Name, m.EffortLevels, m.DefaultEffort))
            .ToList();
        return new ConceptSettingsDto(s.ProfileId, roles, s.Tone, s.Audience, s.Narration, s.WordCheck, selectable);
    }
}
