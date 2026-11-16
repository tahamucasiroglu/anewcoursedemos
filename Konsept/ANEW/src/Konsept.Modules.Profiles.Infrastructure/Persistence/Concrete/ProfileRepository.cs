using Konsept.Modules.Profiles.Application.Abstract;
using Konsept.Modules.Profiles.Domain.Entities;
using Konsept.Shared.Infrastructure.Persistence.Base;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Konsept.Modules.Profiles.Infrastructure.Persistence.Concrete;

/// <summary>
/// Profillerin EF Core + PostgreSQL uygulaması. Her sorgu <c>user_id</c> ile süzülür (BR-1, AC-15 — kasıtlı bozma hedefi). Ad
/// tekilliği yarışında veritabanının tekil indeksi son sözü söyler (<c>23505</c> → <c>false</c>).
/// </summary>
public sealed class ProfileRepository : EfRepositoryBase<ProfilesDbContext>, IProfileRepository
{
    /// <summary>Initializes a new instance of the <see cref="ProfileRepository"/> class.</summary>
    /// <param name="context">Profiles veritabanı bağlamı.</param>
    public ProfileRepository(ProfilesDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Profile>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        await Context.Profiles.AsNoTracking().Where(p => p.UserId == userId).ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<Profile?> GetAsync(Guid userId, Guid profileId, CancellationToken cancellationToken) =>
        Context.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId && p.Id == profileId, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> AddAsync(Profile profile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var entry = Context.Profiles.Add(profile);
        try
        {
            await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return false;
        }
        finally
        {
            entry.State = EntityState.Detached;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(Profile profile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);
        try
        {
            await Context.Profiles
                .Where(p => p.UserId == profile.UserId && p.Id == profile.Id)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(p => p.Name, profile.Name)
                        .SetProperty(p => p.NameKey, profile.NameKey)
                        .SetProperty(p => p.Language, profile.Language)
                        .SetProperty(p => p.Format, profile.Format)
                        .SetProperty(p => p.DurationSeconds, profile.DurationSeconds)
                        .SetProperty(p => p.Content, profile.Content)
                        .SetProperty(p => p.UpdatedAt, profile.UpdatedAt),
                    cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid userId, Guid profileId, CancellationToken cancellationToken) =>
        await Context.Profiles.Where(p => p.UserId == userId && p.Id == profileId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false) > 0;

    /// <inheritdoc />
    public async Task<Guid?> GetSelectionAsync(Guid userId, CancellationToken cancellationToken) =>
        await Context.Selections.AsNoTracking().Where(s => s.UserId == userId).Select(s => (Guid?)s.ProfileId)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task SetSelectionAsync(Guid userId, Guid profileId, CancellationToken cancellationToken) =>
        Context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO profiles.selections (user_id, profile_id) VALUES ({userId}, {profileId})
            ON CONFLICT (user_id) DO UPDATE SET profile_id = EXCLUDED.profile_id
            """,
            cancellationToken);

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
