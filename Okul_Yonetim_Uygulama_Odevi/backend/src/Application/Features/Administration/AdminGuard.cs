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
    private readonly IRoleAssignmentRepository roleAssignments;
    private readonly ILocalizationService localization;

    public AdminGuard(ICurrentSession session, IUnitOfWork unitOfWork, IRoleAssignmentRepository roleAssignments, ILocalizationService localization)
    {
        this.session = session;
        this.unitOfWork = unitOfWork;
        this.roleAssignments = roleAssignments;
        this.localization = localization;
    }

    /// <summary>Seçili okul ve istek sahibi; kurucu değilse yetki hatası.</summary>
    public async Task<(Guid TenantId, Guid ActorId)> RequireFounderAsync()
    {
        if (!this.session.IsFounder || this.session.TenantId is not { } tenantId || this.session.UserId is not { } actorId)
        {
            throw new ForbiddenException(await this.localization.GetAsync(AdministrationMessages.Forbidden).ConfigureAwait(false));
        }

        return (tenantId, actorId);
    }

    /// <summary>
    /// Okulu (ve verildiyse kişiyi) sıraya sokar, sonra istek sahibinin hâlâ bu okulda etkin kurucu/yönetici olduğunu sıra
    /// içinde yeniden denetler (review L-1: yetkisi az önce alınan yönetici, beklemedeki isteğiyle işlem yapamaz). Kişi
    /// anahtarı (review L-2) "başka okulda kayıtlı mı" kontrolünü, aynı kişiyi başka okula ekleme ile sıraya sokar.
    /// </summary>
    public async Task<IExclusiveScope> LockAsync(Guid tenantId, Guid actorId, Guid? memberId, CancellationToken cancellationToken)
    {
        List<string> keys = ["tenant-admin:" + tenantId.ToString("N")];
        if (memberId is { } member)
        {
            keys.Add("member-admin:" + member.ToString("N"));
        }

        IExclusiveScope scope;
        try
        {
            scope = await this.unitOfWork.BeginExclusiveAsync(keys, cancellationToken).ConfigureAwait(false);
        }
        catch (ExclusiveLockTimeoutException)
        {
            throw new BusinessException(await this.localization.GetAsync(AdministrationMessages.TooManyRequests).ConfigureAwait(false));
        }

        bool stillFounder;
        try
        {
            IReadOnlyList<Domain.Identity.RoleAssignment> roles =
                await this.roleAssignments.ListForUserInTenantAsync(actorId, tenantId, cancellationToken).ConfigureAwait(false);
            stillFounder = roles.Any(r => r.Role == Domain.Identity.Role.Founder);
        }
        catch
        {
            // Sorgu hata verirse (ör. istek iptali) sıra hemen bırakılır (review round 2, I-B).
            await scope.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        if (!stillFounder)
        {
            await scope.DisposeAsync().ConfigureAwait(false);
            throw new ForbiddenException(await this.localization.GetAsync(AdministrationMessages.Forbidden).ConfigureAwait(false));
        }

        return scope;
    }
}
