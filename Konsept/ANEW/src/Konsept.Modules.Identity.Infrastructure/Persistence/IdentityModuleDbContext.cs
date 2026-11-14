using Konsept.Modules.Identity.Domain.Entities;
using Konsept.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Konsept.Modules.Identity.Infrastructure.Persistence;

/// <summary>
/// Identity modülünün veritabanı bağlamı (<c>identity</c> şeması): kullanıcılar ve oturumlar. ASP.NET Core Identity'nin
/// <c>IdentityDbContext</c>'i kullanılmaz (plan Risk 1).
/// </summary>
public sealed class IdentityModuleDbContext : ModuleDbContextBase
{
    /// <summary>Şema adı.</summary>
    public const string SchemaName = "identity";

    /// <summary>Initializes a new instance of the <see cref="IdentityModuleDbContext"/> class.</summary>
    /// <param name="options">EF Core seçenekleri.</param>
    public IdentityModuleDbContext(DbContextOptions<IdentityModuleDbContext> options)
        : base(options)
    {
    }

    /// <summary>Gets kullanıcılar.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Gets oturumlar.</summary>
    public DbSet<UserSession> Sessions => Set<UserSession>();

    /// <inheritdoc />
    protected override string Schema => SchemaName;
}
