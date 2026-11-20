using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Application.Features.Administration;

/// <summary>
/// Yönetim işlemlerinin ortak kapısı (spec 0005): istek sahibi seçili okulda kurucu/yönetici olmalı (API politikasına ek
/// savunma; değilse reddedilir). Rol ve üyelik değiştiren işlemler okul başına sıraya sokulur: "en az bir etkin yönetici"
/// kuralı aynı anda gelen işlemlerle bozulamaz (Ö-5).
/// </summary>
public sealed class AdminGuard
{
    private readonly ICurrentSession session;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILocalizationService localization;

    public AdminGuard(ICurrentSession session, IUnitOfWork unitOfWork, ILocalizationService localization)
    {
        this.session = session;
        this.unitOfWork = unitOfWork;
        this.localization = localization;
    }

    /// <summary>Seçili okul ve istek sahibi; kurucu değilse yetki hatası.</summary>
    public async Task<(Guid TenantId, Guid ActorId)> RequireFounderAsync()
    {
        if (!this.session.IsFounder || this.session.TenantId is not { } tenantId || this.session.UserId is not { } actorId)
        {
            throw new AuthorizationException(await this.localization.GetAsync(AdministrationMessages.Forbidden).ConfigureAwait(false));
        }

        return (tenantId, actorId);
    }

    public async Task<IExclusiveScope> LockTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        try
        {
            return await this.unitOfWork.BeginExclusiveAsync("tenant-admin:" + tenantId.ToString("N"), cancellationToken).ConfigureAwait(false);
        }
        catch (ExclusiveLockTimeoutException)
        {
            throw new BusinessException(await this.localization.GetAsync(AdministrationMessages.TooManyRequests).ConfigureAwait(false));
        }
    }
}
