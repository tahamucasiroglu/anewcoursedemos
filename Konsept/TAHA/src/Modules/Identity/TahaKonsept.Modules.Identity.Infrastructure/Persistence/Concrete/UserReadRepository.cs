using Microsoft.EntityFrameworkCore;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Modules.Identity.Domain.Entities;
using TahaKonsept.Modules.Identity.Infrastructure.Persistence.Base;

namespace TahaKonsept.Modules.Identity.Infrastructure.Persistence.Concrete;

/// <summary>
/// <see cref="IUserReadRepository"/> EF uygulaması: kullanıcı sayımı/listesi ve rol üyeliği sorguları (izlenmeyen).
/// </summary>
public sealed class UserReadRepository : IdentityRepositoryBase<AppUser>, IUserReadRepository
{
    /// <summary>Oluşturur.</summary>
    /// <param name="context">Identity DbContext.</param>
    public UserReadRepository(IdentityModuleDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public Task<int> CountAsync(CancellationToken ct = default) => Set.CountAsync(ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AppUser>> ListAsync(CancellationToken ct = default) =>
        await QueryNoTracking().OrderBy(u => u.UserName).ToListAsync(ct);

    /// <inheritdoc />
    public Task<int> CountActiveInRoleAsync(string roleName, CancellationToken ct = default) =>
        UsersInRole(roleName).CountAsync(u => !u.IsBanned, ct);

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> ListUserIdsInRoleAsync(string roleName, CancellationToken ct = default) =>
        (await UsersInRole(roleName).Select(u => u.Id).ToListAsync(ct)).ToHashSet();

    private IQueryable<AppUser> UsersInRole(string roleName)
    {
        var normalized = roleName.ToUpperInvariant();
        return from user in Context.Users.AsNoTracking()
               join userRole in Context.UserRoles on user.Id equals userRole.UserId
               join role in Context.Roles on userRole.RoleId equals role.Id
               where role.NormalizedName == normalized
               select user;
    }
}
