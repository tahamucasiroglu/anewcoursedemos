using Konsept.Modules.Profiles.Application.Abstract;
using Konsept.Modules.Profiles.Domain.Entities;
using Konsept.Shared.Infrastructure.Persistence.Base;
using Microsoft.EntityFrameworkCore;

namespace Konsept.Modules.Profiles.Infrastructure.Persistence.Concrete;

/// <summary>
/// Konsept ayarlarının EF Core + PostgreSQL uygulaması (spec 0005). Okuma <c>user_id</c> ile süzülür; yazma tek ifadelik upsert'tür
/// ve var olan satır yalnızca aynı kullanıcınınsa güncellenir (BR-1).
/// </summary>
public sealed class ConceptSettingsRepository : EfRepositoryBase<ProfilesDbContext>, IConceptSettingsRepository
{
    /// <summary>Initializes a new instance of the <see cref="ConceptSettingsRepository"/> class.</summary>
    /// <param name="context">Profiles veritabanı bağlamı.</param>
    public ConceptSettingsRepository(ProfilesDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public Task<ConceptSettings?> GetAsync(Guid userId, Guid profileId, CancellationToken cancellationToken) =>
        Context.ConceptSettings.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId && s.ProfileId == profileId, cancellationToken);

    /// <inheritdoc />
    public Task SaveAsync(ConceptSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return Context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO profiles.concept_settings
                (profile_id, user_id, concept_model, concept_effort, dedupe_model, dedupe_effort, topic_model, topic_effort,
                 tone, audience, narration, word_check, updated_at)
            VALUES ({settings.ProfileId}, {settings.UserId}, {settings.ConceptModel}, {settings.ConceptEffort}, {settings.DedupeModel},
                    {settings.DedupeEffort}, {settings.TopicModel}, {settings.TopicEffort}, {settings.Tone}, {settings.Audience},
                    {settings.Narration}, {settings.WordCheck}, {settings.UpdatedAt})
            ON CONFLICT (profile_id) DO UPDATE SET
                concept_model = EXCLUDED.concept_model, concept_effort = EXCLUDED.concept_effort,
                dedupe_model = EXCLUDED.dedupe_model, dedupe_effort = EXCLUDED.dedupe_effort,
                topic_model = EXCLUDED.topic_model, topic_effort = EXCLUDED.topic_effort,
                tone = EXCLUDED.tone, audience = EXCLUDED.audience, narration = EXCLUDED.narration,
                word_check = EXCLUDED.word_check, updated_at = EXCLUDED.updated_at
            WHERE profiles.concept_settings.user_id = EXCLUDED.user_id
            """,
            cancellationToken);
    }
}
