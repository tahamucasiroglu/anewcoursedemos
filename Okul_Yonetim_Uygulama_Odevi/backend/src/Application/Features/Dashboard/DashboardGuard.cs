using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Organization.Queries.GetBranches;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Application.Features.Dashboard;

/// <summary>
/// Yönetici panelinin yetki kapısı (spec 0017 gereksinim 1–2, C-1; <c>AdminGuard.RequireFounderAsync</c> kalıbı): panel, raporlar ve dışa
/// aktarma yalnızca seçili okulun kurucusuna açıktır — kurucu bilgisi her istekte veritabanından (oturum doğrulaması), değilse 403 (öğretmen,
/// veli, muhasebe). Şube süzgeci verilirse oturumun gördüğü etkin şubelerden biri olmalı; başka okulun ya da pasif şube 404 (var olduğu
/// sızdırılmaz). API politikasına (<c>TenantFounder</c>) ek savunmadır.
/// </summary>
public sealed class DashboardGuard
{
    private readonly ICurrentSession session;
    private readonly ISender sender;
    private readonly ILocalizationService localization;

    public DashboardGuard(ICurrentSession session, ISender sender, ILocalizationService localization)
    {
        this.session = session;
        this.sender = sender;
        this.localization = localization;
    }

    /// <summary>Kurucu ve (verildiyse) şube görünür; seçili okul ve istek sahibi.</summary>
    public async Task<(Guid TenantId, Guid UserId)> RequireAsync(Guid? branchId, CancellationToken cancellationToken)
    {
        if (!this.session.IsFounder || this.session.TenantId is not { } tenantId || this.session.UserId is not { } userId)
        {
            throw new ForbiddenException(await this.localization.DashboardTextAsync(DashboardMessages.Forbidden).ConfigureAwait(false));
        }

        if (branchId is { } branch
            && !(await this.sender.Send(new GetBranchesQuery(), cancellationToken).ConfigureAwait(false)).Any(b => b.Id == branch))
        {
            throw new NotFoundException(await this.localization.DashboardTextAsync(DashboardMessages.BranchNotFound).ConfigureAwait(false));
        }

        return (tenantId, userId);
    }
}
