using Konsept.Modules.Profiles.Application.Abstract;
using Konsept.Modules.Profiles.Contracts.Abstract;
using Konsept.Modules.Profiles.Contracts.Dtos;
using Konsept.Modules.Profiles.Domain.Rules;

namespace Konsept.Modules.Profiles.Application.Base;

/// <summary>
/// Profiles.Contracts <see cref="IProfileGenerationReader"/> (spec 0014): profil ve konsept ayarları kullanıcı kimliğiyle okunur (BR-1);
/// sistem talimatı <see cref="ProfileInstruction.Build"/>'dir. Ayar satırı yoksa varsayılandır (roller seçilmedi, ince ayar
/// Belirtilmedi, kelime denetimi kapalı).
/// </summary>
/// <param name="profiles">Profil deposu.</param>
/// <param name="settings">Konsept ayarı deposu.</param>
public abstract class ProfileGenerationReaderBase(IProfileRepository profiles, IConceptSettingsRepository settings) : IProfileGenerationReader
{
    /// <inheritdoc />
    public async Task<ProfileGenerationContext?> GetAsync(Guid userId, Guid profileId, CancellationToken cancellationToken)
    {
        if (await profiles.GetAsync(userId, profileId, cancellationToken).ConfigureAwait(false) is not { } profile)
        {
            return null;
        }

        var row = await settings.GetAsync(userId, profileId, cancellationToken).ConfigureAwait(false);
        return new ProfileGenerationContext
        {
            ProfileId = profile.Id,
            Name = profile.Name,
            Language = profile.Language,
            Format = profile.Format,
            DurationSeconds = profile.DurationSeconds,
            Instruction = ProfileInstruction.Build(profile),
            ConceptModel = row?.ConceptModel,
            ConceptEffort = row?.ConceptEffort,
            DedupeModel = row?.DedupeModel,
            DedupeEffort = row?.DedupeEffort,
            TopicModel = row?.TopicModel,
            TopicEffort = row?.TopicEffort,
            Tone = row?.Tone,
            Audience = row?.Audience,
            Narration = row?.Narration,
            WordCheck = row?.WordCheck ?? false,
        };
    }
}
