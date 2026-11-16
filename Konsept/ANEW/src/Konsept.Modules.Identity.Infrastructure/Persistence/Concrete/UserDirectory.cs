using Konsept.Modules.Identity.Contracts.Abstract;
using Konsept.Modules.Identity.Contracts.Dtos;
using Konsept.Shared.Infrastructure.Persistence.Base;
using Microsoft.EntityFrameworkCore;

namespace Konsept.Modules.Identity.Infrastructure.Persistence.Concrete;

/// <summary>Kullanıcı dizininin EF Core uygulaması (spec 0010): yalnızca kimlik, ad, e-posta ve kayıt anı okunur.</summary>
public sealed class UserDirectory : EfRepositoryBase<IdentityModuleDbContext>, IUserDirectory
{
    /// <summary>Initializes a new instance of the <see cref="UserDirectory"/> class.</summary>
    /// <param name="context">Identity veritabanı bağlamı.</param>
    public UserDirectory(IdentityModuleDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserDirectoryEntry>> ListAsync(CancellationToken cancellationToken) =>
        await Context.Users
            .AsNoTracking()
            .Select(u => new UserDirectoryEntry(u.Id, u.Name, u.Email, u.RegisteredAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
