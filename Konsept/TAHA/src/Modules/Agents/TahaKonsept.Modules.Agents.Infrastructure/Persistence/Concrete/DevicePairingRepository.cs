using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Agents.Domain.Entities;
using TahaKonsept.Modules.Agents.Domain.Enums;

namespace TahaKonsept.Modules.Agents.Infrastructure.Persistence.Concrete;

/// <summary>
/// <see cref="IDevicePairingRepository"/> EF uygulaması.
/// </summary>
public sealed class DevicePairingRepository : Repository<DevicePairing>, IDevicePairingRepository
{
    /// <summary>Oluşturur.</summary>
    /// <param name="context">DbContext.</param>
    public DevicePairingRepository(AgentsDbContext context)
        : base(context)
    {
    }

    /// <summary>Kaydeder; eşzamanlılık çakışmasında değişiklikleri atıp <c>false</c> döner.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kaydedildiyse <c>true</c>.</returns>
    public async Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await Context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            Context.ChangeTracker.Clear();
            return false;
        }
    }

    /// <summary>Cihaz kodu hash'ine göre.</summary>
    /// <param name="deviceCodeHash">Hash.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İstek.</returns>
    public Task<DevicePairing?> FindByDeviceCodeHashAsync(string deviceCodeHash, CancellationToken cancellationToken = default) =>
        Query().FirstOrDefaultAsync(p => p.DeviceCodeHash == deviceCodeHash, cancellationToken);

    /// <summary>Kullanıcı koduna göre açık istek.</summary>
    /// <param name="userCode">Kod.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İstek.</returns>
    public Task<DevicePairing?> FindOpenByUserCodeAsync(string userCode, CancellationToken cancellationToken = default) =>
        Query()
            .Where(p => p.UserCode == userCode && (p.Status == DevicePairingStatus.Pending || p.Status == DevicePairingStatus.Approved))
            .OrderByDescending(p => p.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>Kod kullanımda mı.</summary>
    /// <param name="userCode">Kod.</param>
    /// <param name="now">Şu an.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kullanımdaysa <c>true</c>.</returns>
    public Task<bool> IsUserCodeInUseAsync(string userCode, DateTimeOffset now, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(
            p => p.UserCode == userCode
                && p.ExpiresAtUtc > now
                && (p.Status == DevicePairingStatus.Pending || p.Status == DevicePairingStatus.Approved),
            cancellationToken);

    /// <summary>Süresi dolmuşları silinmek üzere işaretler.</summary>
    /// <param name="before">Sınır.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İşaretlenen sayısı.</returns>
    public async Task<int> DeleteExpiredAsync(DateTimeOffset before, CancellationToken cancellationToken = default)
    {
        var expired = await Query().Where(p => p.ExpiresAtUtc < before).Take(500).ToListAsync(cancellationToken);
        Set.RemoveRange(expired);
        return expired.Count;
    }
}
