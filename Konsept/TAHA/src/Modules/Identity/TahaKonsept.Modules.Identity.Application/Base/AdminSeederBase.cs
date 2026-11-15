using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Modules.Identity.Application.Models;

namespace TahaKonsept.Modules.Identity.Application.Base;

/// <summary>
/// <see cref="IAdminSeeder"/> tabanı: ayarları tutar ve özet biçimini denetler (<see cref="IsIdentityV3Hash"/>: base64, ilk bayt 0x01,
/// en az 13 + 16 + 32 bayt). Oluşturma akışı türetilen sınıftadır.
/// </summary>
public abstract class AdminSeederBase : IAdminSeeder
{
    /// <summary>Ayarları alır.</summary>
    /// <param name="options">Tohum ayarları.</param>
    protected AdminSeederBase(AdminSeedOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>Tohum ayarları.</summary>
    protected AdminSeedOptions Options { get; }

    /// <inheritdoc />
    public abstract Task<bool> SeedAsync(CancellationToken ct = default);

    /// <summary>Metin ASP.NET Identity v3 parola özeti biçiminde mi (içerik doğrulanmaz, yalnız biçim).</summary>
    /// <param name="hash">Özet.</param>
    /// <returns>Biçim uygunsa <c>true</c>.</returns>
    protected static bool IsIdentityV3Hash(string hash)
    {
        var buffer = new byte[hash.Length];
        return Convert.TryFromBase64String(hash.Trim(), buffer, out var length) && length >= 13 + 16 + 32 && buffer[0] == 0x01;
    }
}
