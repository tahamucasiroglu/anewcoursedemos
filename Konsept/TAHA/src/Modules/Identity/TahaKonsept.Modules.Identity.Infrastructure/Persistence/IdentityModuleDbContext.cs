using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Modules.Identity.Domain.Entities;

namespace TahaKonsept.Modules.Identity.Infrastructure.Persistence;

/// <summary>
/// Identity modülünün DbContext'i: ASP.NET Core Identity tabloları + <see cref="RefreshTokens"/>. <c>IdentityDbContext</c>'ten
/// türediği için <c>ModuleDbContextBase</c> kullanılamaz; şema (<see cref="SchemaName"/>) ve yapılandırmalar burada uygulanır.
/// Tablo adları kısaltılır (<c>users</c>, <c>roles</c>, <c>user_roles</c>…); sütunlar snake_case (kayıtta <c>UseSnakeCaseNamingConvention</c>).
/// <see cref="IIdentityUnitOfWork"/> olarak da kullanılır.
/// </summary>
public class IdentityModuleDbContext : IdentityDbContext<AppUser, AppRole, Guid>, IIdentityUnitOfWork
{
    /// <summary>Veritabanı şeması.</summary>
    public const string SchemaName = "identity";

    /// <summary>Oluşturur.</summary>
    /// <param name="options">EF seçenekleri.</param>
    public IdentityModuleDbContext(DbContextOptions<IdentityModuleDbContext> options)
        : base(options)
    {
    }

    /// <summary>Refresh token'lar.</summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>Kaydeder; eşzamanlılık çakışmasında izlenen değişiklikleri atıp <c>false</c> döner.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kaydedildiyse <c>true</c>.</returns>
    public async Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            ChangeTracker.Clear();
            return false;
        }
    }

    /// <summary>Identity modelini kurar, şemayı ve tablo adlarını ayarlar, bu derlemedeki yapılandırmaları uygular.</summary>
    /// <param name="builder">Model oluşturucu.</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(SchemaName);

        builder.Entity<AppUser>().ToTable("users");
        builder.Entity<AppRole>().ToTable("roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");

        builder.ApplyConfigurationsFromAssembly(typeof(IdentityModuleDbContext).Assembly);
    }
}
