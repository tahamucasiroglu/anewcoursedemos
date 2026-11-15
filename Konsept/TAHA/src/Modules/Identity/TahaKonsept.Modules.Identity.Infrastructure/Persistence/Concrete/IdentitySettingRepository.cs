using Microsoft.EntityFrameworkCore;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Modules.Identity.Domain.Entities;
using TahaKonsept.Modules.Identity.Infrastructure.Persistence.Base;

namespace TahaKonsept.Modules.Identity.Infrastructure.Persistence.Concrete;

/// <summary>
/// <see cref="IIdentitySettingRepository"/> EF uygulaması. <see cref="TryAddAsync"/> önce var olup olmadığına bakar, sonra ekleyip kaydeder;
/// eşzamanlı eklemede birincil anahtar çakışması (<see cref="DbUpdateException"/>) yakalanır, eklenen kayıt ayrılır ve <c>false</c> döner.
/// </summary>
public sealed class IdentitySettingRepository : IdentityRepositoryBase<IdentitySetting>, IIdentitySettingRepository
{
    /// <summary>Oluşturur.</summary>
    /// <param name="context">Identity DbContext.</param>
    public IdentitySettingRepository(IdentityModuleDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public Task<IdentitySetting?> FindAsync(string key, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(s => s.Key == key, ct);

    /// <inheritdoc />
    public async Task<bool> TryAddAsync(IdentitySetting setting, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(setting);
        if (await QueryNoTracking().AnyAsync(s => s.Key == setting.Key, ct))
        {
            return false;
        }

        var entry = Set.Add(setting);
        try
        {
            await Context.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException)
        {
            entry.State = EntityState.Detached;
            return false;
        }
    }

    /// <inheritdoc />
    public Task SaveAsync(CancellationToken ct = default) => Context.SaveChangesAsync(ct);
}
