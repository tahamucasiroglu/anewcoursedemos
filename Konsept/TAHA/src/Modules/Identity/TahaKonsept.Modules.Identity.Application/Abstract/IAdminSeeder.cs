using TahaKonsept.Modules.Identity.Application.Models;

namespace TahaKonsept.Modules.Identity.Application.Abstract;

/// <summary>
/// Yönetici tohumu (API başlangıcında, migration'lardan sonra): <see cref="AdminSeedOptions"/> e-postasıyla kullanıcı yoksa
/// <c>Admin</c> rolünde oluşturur; varsa hiçbir şeye dokunmaz (idempotent). Ayar yoksa ya da özet biçimi geçersizse atlanır ve loglanır
/// (e-posta / özet loglanmaz).
/// </summary>
public interface IAdminSeeder
{
    /// <summary>Tohumu uygular.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Yeni yönetici oluşturulduysa <c>true</c>.</returns>
    Task<bool> SeedAsync(CancellationToken ct = default);
}
