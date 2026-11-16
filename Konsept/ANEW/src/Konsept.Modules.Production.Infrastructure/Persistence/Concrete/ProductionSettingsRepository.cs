using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Domain.Entities;
using Konsept.Shared.Infrastructure.Persistence.Base;
using Microsoft.EntityFrameworkCore;

namespace Konsept.Modules.Production.Infrastructure.Persistence.Concrete;

/// <summary>Üretim ayarları deposu: tek ifadelik upsert; başka kullanıcının profil satırı değişmez (BR-1).</summary>
public sealed class ProductionSettingsRepository : EfRepositoryBase<ProductionDbContext>, IProductionSettingsRepository
{
    /// <summary>Initializes a new instance of the <see cref="ProductionSettingsRepository"/> class.</summary>
    /// <param name="context">Bağlam.</param>
    public ProductionSettingsRepository(ProductionDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public Task<ProductionSettings?> GetAsync(Guid userId, Guid profileId, CancellationToken cancellationToken) =>
        Context.Settings.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId && s.ProfileId == profileId, cancellationToken);

    /// <inheritdoc />
    public Task UpsertAsync(ProductionSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return Context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO production.settings
                (profile_id, user_id, subtitles, progress_bar, code_model, code_effort, engine, image_model, audio_model, updated_at)
            VALUES ({settings.ProfileId}, {settings.UserId}, {settings.Subtitles}, {settings.ProgressBar}, {settings.CodeModel},
                    {settings.CodeEffort}, {settings.Engine}, {settings.ImageModel}, {settings.AudioModel}, {settings.UpdatedAt})
            ON CONFLICT (profile_id) DO UPDATE SET
                subtitles = EXCLUDED.subtitles, progress_bar = EXCLUDED.progress_bar, code_model = EXCLUDED.code_model,
                code_effort = EXCLUDED.code_effort, engine = EXCLUDED.engine, image_model = EXCLUDED.image_model,
                audio_model = EXCLUDED.audio_model, updated_at = EXCLUDED.updated_at
            WHERE production.settings.user_id = EXCLUDED.user_id
            """,
            cancellationToken);
    }
}
