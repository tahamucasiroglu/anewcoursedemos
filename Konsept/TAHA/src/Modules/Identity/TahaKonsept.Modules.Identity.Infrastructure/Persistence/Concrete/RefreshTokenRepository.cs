using Microsoft.EntityFrameworkCore;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Modules.Identity.Domain.Entities;
using TahaKonsept.Modules.Identity.Infrastructure.Persistence.Base;

namespace TahaKonsept.Modules.Identity.Infrastructure.Persistence.Concrete;

/// <summary>
/// <see cref="IRefreshTokenRepository"/> EF uygulaması (izlenen varlıklar; kaydetme iş biriminde).
/// </summary>
public sealed class RefreshTokenRepository : IdentityRepositoryBase<RefreshToken>, IRefreshTokenRepository
{
    /// <summary>Oluşturur.</summary>
    /// <param name="context">Identity DbContext.</param>
    public RefreshTokenRepository(IdentityModuleDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task AddAsync(RefreshToken token, CancellationToken ct = default) => await Set.AddAsync(token, ct);

    /// <inheritdoc />
    public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RefreshToken>> ListUnrevokedByFamilyAsync(Guid familyId, CancellationToken ct = default) =>
        await Set.Where(t => t.FamilyId == familyId && t.RevokedAtUtc == null).ToListAsync(ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RefreshToken>> ListUnrevokedByUserAsync(Guid userId, CancellationToken ct = default) =>
        await Set.Where(t => t.UserId == userId && t.RevokedAtUtc == null).ToListAsync(ct);
}
