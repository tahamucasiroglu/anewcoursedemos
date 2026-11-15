using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Identity.Application.Base;
using TahaKonsept.Modules.Identity.Application.Models;
using TahaKonsept.Modules.Identity.Domain.Entities;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Application.Concrete;

/// <summary>
/// Yönetici tohumu: e-postayla kullanıcı yoksa kullanıcı adı = e-posta, onaylı e-posta, <c>PasswordHash</c> = verilen özet
/// (parola politikası ve yeniden özetleme YOK; <c>UserManager.CreateAsync(user)</c> parolasız aşırı yüklemesi), yeni güvenlik/eşzamanlılık
/// damgası ve <see cref="RoleNames.Admin"/> rolüyle oluşturur. Kullanıcı varsa dokunmaz. Değerler loglanmaz.
/// </summary>
public sealed class AdminSeeder : AdminSeederBase
{
    private readonly UserManager<AppUser> _users;
    private readonly RoleManager<AppRole> _roles;
    private readonly IClock _clock;
    private readonly ILogger<AdminSeeder> _logger;

    /// <summary>Bağımlılıkları alır.</summary>
    /// <param name="options">Tohum ayarları.</param>
    /// <param name="users">Kullanıcı yöneticisi.</param>
    /// <param name="roles">Rol yöneticisi.</param>
    /// <param name="clock">Saat.</param>
    /// <param name="logger">Günlükleyici.</param>
    public AdminSeeder(AdminSeedOptions options, UserManager<AppUser> users, RoleManager<AppRole> roles, IClock clock, ILogger<AdminSeeder> logger)
        : base(options)
    {
        _users = users;
        _roles = roles;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<bool> SeedAsync(CancellationToken ct = default)
    {
        if (!Options.IsConfigured)
        {
            _logger.LogInformation("Yönetici tohumu yapılandırılmamış ({EmailKey} / {HashKey}); atlandı.", AdminSeedOptions.EmailKey, AdminSeedOptions.PasswordHashKey);
            return false;
        }

        var hash = Options.PasswordHash!.Trim();
        if (!IsIdentityV3Hash(hash))
        {
            _logger.LogError("Yönetici tohumu atlandı: {HashKey} geçerli bir ASP.NET Identity v3 özeti değil.", AdminSeedOptions.PasswordHashKey);
            return false;
        }

        var email = Options.Email!.Trim();
        if (await _users.FindByEmailAsync(email) is not null)
        {
            _logger.LogInformation("Yönetici tohumu: hesap zaten var, dokunulmadı.");
            return false;
        }

        if (!await _roles.RoleExistsAsync(RoleNames.Admin))
        {
            var role = await _roles.CreateAsync(new AppRole(RoleNames.Admin));
            if (!role.Succeeded)
            {
                _logger.LogError("Yönetici tohumu: Admin rolü oluşturulamadı ({Errors}).", Describe(role));
                return false;
            }
        }

        var user = new AppUser(email, _clock.UtcNow)
        {
            Email = email,
            EmailConfirmed = true,
            PasswordHash = hash,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("D"),
        };

        var created = await _users.CreateAsync(user);
        if (!created.Succeeded)
        {
            _logger.LogError("Yönetici tohumu: hesap oluşturulamadı ({Errors}).", Describe(created));
            return false;
        }

        var inRole = await _users.AddToRoleAsync(user, RoleNames.Admin);
        if (!inRole.Succeeded)
        {
            _logger.LogError("Yönetici tohumu: rol atanamadı ({Errors}).", Describe(inRole));
            return false;
        }

        _logger.LogInformation("Yönetici tohumu: yönetici hesabı oluşturuldu ({UserId}).", user.Id);
        return true;
    }

    private static string Describe(IdentityResult result) => string.Join(", ", result.Errors.Select(e => e.Code));
}
