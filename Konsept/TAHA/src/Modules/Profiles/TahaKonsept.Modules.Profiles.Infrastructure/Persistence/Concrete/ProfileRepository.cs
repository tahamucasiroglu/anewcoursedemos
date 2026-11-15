using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;
using TahaKonsept.Modules.Profiles.Application.Abstract;
using TahaKonsept.Modules.Profiles.Domain.Entities;

namespace TahaKonsept.Modules.Profiles.Infrastructure.Persistence.Concrete;

/// <summary>
/// <see cref="IProfileRepository"/>'nin EF Core uygulaması (kullanıcıya göre filtreli sorgular).
/// </summary>
/// <param name="context">Modül DbContext'i.</param>
public sealed class ProfileRepository(ProfilesDbContext context) : Repository<Profile>(context), IProfileRepository
{
    /// <summary>Kullanıcının profilleri (takipsiz, eskiden yeniye).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Profiller.</returns>
    public async Task<IReadOnlyList<Profile>> ListForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await QueryNoTracking()
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.CreatedAtUtc).ThenBy(p => p.Id)
            .ToListAsync(cancellationToken);

    /// <summary>Kullanıcıya ait profili takipli getirir.</summary>
    /// <param name="id">Profil kimliği.</param>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Profil ya da <c>null</c>.</returns>
    public Task<Profile?> GetForUserTrackedAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) =>
        Query().FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, cancellationToken);

    /// <summary>Kullanıcının aktif profillerini takipli getirir.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Aktif profiller.</returns>
    public async Task<IReadOnlyList<Profile>> ListActiveTrackedAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await Query().Where(p => p.UserId == userId && p.IsActive).ToListAsync(cancellationToken);

    /// <summary>Verilen profil dışındaki en yeni profili takipli getirir.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="excludeId">Hariç tutulacak profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Profil ya da <c>null</c>.</returns>
    public Task<Profile?> GetNewestOtherTrackedAsync(Guid userId, Guid excludeId, CancellationToken cancellationToken = default) =>
        Query()
            .Where(p => p.UserId == userId && p.Id != excludeId)
            .OrderByDescending(p => p.CreatedAtUtc).ThenByDescending(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>Kullanıcının aktif profilini takipsiz getirir.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Profil ya da <c>null</c>.</returns>
    public Task<Profile?> GetActiveAsync(Guid userId, CancellationToken cancellationToken = default) =>
        QueryNoTracking()
            .Where(p => p.UserId == userId && p.IsActive)
            .OrderByDescending(p => p.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>Kullanıcıya ait profili kimliğe göre takipsiz getirir.</summary>
    /// <param name="id">Profil kimliği.</param>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Profil ya da <c>null</c>.</returns>
    public Task<Profile?> FindForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) =>
        QueryNoTracking().FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, cancellationToken);
}
