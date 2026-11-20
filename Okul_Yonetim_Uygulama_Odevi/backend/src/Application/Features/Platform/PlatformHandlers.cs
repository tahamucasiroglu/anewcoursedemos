using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Identity.Commands.CreateSchool;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Features.Platform;

/// <summary>
/// Platform paneli (spec 0007): okul listesi (özet), okul açma (okul açma komutunun kurallarıyla; yeni kurucuya geçici
/// şifre bir kez) ve okulu pasifleştirme / etkinleştirme. Yalnızca platform yöneticisi ve kodla açılmış oturum (API
/// politikasına ek savunma). Okulların içindeki veriye erişim yok. Her işlem audit'e yazılır.
/// </summary>
public sealed class PlatformHandlers :
    IRequestHandler<ListSchoolsQuery, SchoolPage>,
    IRequestHandler<CreateSchoolFromPanelCommand, PanelSchoolResult>,
    IRequestHandler<SetSchoolActiveCommand, SchoolRow>
{
    public const int PageSize = 25;

    private readonly ICurrentSession session;
    private readonly IPlatformDirectory directory;
    private readonly IUserRepository users;
    private readonly ITemporaryPasswordGenerator passwords;
    private readonly ISender sender;
    private readonly IAuditLog audit;
    private readonly IClientInfo client;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<PlatformHandlers> logger;

    public PlatformHandlers(
        ICurrentSession session,
        IPlatformDirectory directory,
        IUserRepository users,
        ITemporaryPasswordGenerator passwords,
        ISender sender,
        IAuditLog audit,
        IClientInfo client,
        IUnitOfWork unitOfWork,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<PlatformHandlers> logger)
    {
        this.session = session;
        this.directory = directory;
        this.users = users;
        this.passwords = passwords;
        this.sender = sender;
        this.audit = audit;
        this.client = client;
        this.unitOfWork = unitOfWork;
        this.localization = localization;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<SchoolPage> Handle(ListSchoolsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await this.RequirePlatformAdminAsync().ConfigureAwait(false);
        bool? active = request.Status switch { "active" => true, "inactive" => false, _ => null };
        return await this.directory.ListAsync(new SchoolListFilter(string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim(), active, Math.Max(1, request.Page), PageSize), cancellationToken).ConfigureAwait(false);
    }

    public async Task<PanelSchoolResult> Handle(CreateSchoolFromPanelCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Guid actorId = await this.RequirePlatformAdminAsync().ConfigureAwait(false);
        bool existing = await this.users.FindByEmailAsync(EmailAddress.Normalize(request.FounderEmail), cancellationToken).ConfigureAwait(false) is not null;
        string? temporary = existing ? null : this.passwords.Generate();
        CreateSchoolResult result = await this.sender.Send(
            new CreateSchoolCommand(request.SchoolName, request.BranchName, request.FounderName, request.FounderEmail, temporary, "platform paneli", actorId, this.client.IpAddress),
            cancellationToken).ConfigureAwait(false);
        if (result.AlreadyExisted)
        {
            throw new BusinessException(await this.localization.GetAsync(PlatformMessages.SchoolExists).ConfigureAwait(false));
        }

        this.logger.LogInformation("Platform paneli: okul {TenantId} açıldı (yeni kurucu hesabı: {NewAccount})", result.TenantId, result.UserCreated);
        return new PanelSchoolResult(result.TenantId, !result.UserCreated, result.UserCreated ? temporary : null);
    }

    public async Task<SchoolRow> Handle(SetSchoolActiveCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Guid actorId = await this.RequirePlatformAdminAsync().ConfigureAwait(false);
        Tenant tenant = await this.directory.FindTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(await this.localization.GetAsync(PlatformMessages.SchoolNotFound).ConfigureAwait(false));
        if (tenant.IsActive != request.Active)
        {
            if (request.Active)
            {
                tenant.Activate();
            }
            else
            {
                tenant.Deactivate();
            }

            this.audit.Add(AuditEntry.Record(request.Active ? AuditAction.TenantActivated : AuditAction.TenantDeactivated, this.timeProvider.GetUtcNow(), tenant.Id, actorId, targetType: nameof(Tenant), targetId: tenant.Id, ipAddress: this.client.IpAddress));
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("Platform paneli: okul {TenantId} {State}", tenant.Id, request.Active ? "etkinleştirildi" : "pasifleştirildi");
        }

        return await this.directory.FindAsync(tenant.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(await this.localization.GetAsync(PlatformMessages.SchoolNotFound).ConfigureAwait(false));
    }

    private async Task<Guid> RequirePlatformAdminAsync()
    {
        if (!this.session.IsPlatformAdmin || this.session.UserId is not { } actorId)
        {
            throw new ForbiddenException(await this.localization.GetAsync(PlatformMessages.Forbidden).ConfigureAwait(false));
        }

        return actorId;
    }
}
