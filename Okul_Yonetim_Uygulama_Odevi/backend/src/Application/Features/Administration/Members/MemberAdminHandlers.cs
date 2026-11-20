using FluentValidation;
using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Features.Administration.Members;

/// <summary>
/// Kişi yönetimi (spec 0005 gereksinim 3–10): ekleme (yeni kişiye bir kez gösterilen geçici şifre; var olan kişiye
/// yalnızca rol), profil, roller, pasifleştirme ve kurucunun şifre sıfırlaması. Yalnızca seçili okulun kurucusu; kişi bu
/// okulda değilse "bulunamadı". Değişiklikler okul başına sıralıdır: "en az bir etkin yönetici", "kendi rolünü/kendini"
/// korumaları ve tekrar ekleme kontrolü eşzamanlı isteklerle atlatılamaz. Geçici şifre yalnızca yanıtta döner.
/// </summary>
public sealed class MemberAdminHandlers :
    IRequestHandler<AddMemberCommand, AddMemberResult>,
    IRequestHandler<UpdateMemberProfileCommand, MemberDetail>,
    IRequestHandler<SetMemberRolesCommand, MemberDetail>,
    IRequestHandler<SetMemberActiveCommand, MemberDetail>,
    IRequestHandler<ResetMemberPasswordCommand, ResetMemberPasswordResult>,
    IRequestHandler<ListMembersQuery, MemberPage>,
    IRequestHandler<GetMemberQuery, MemberDetail>
{
    public const int PageSize = 25;

    private readonly AdminGuard guard;
    private readonly AdminAudit audit;
    private readonly IUserRepository users;
    private readonly IBranchRepository branches;
    private readonly IRoleAssignmentRepository roleAssignments;
    private readonly ITenantMembershipRepository memberships;
    private readonly IUserSessionRepository sessions;
    private readonly IPasswordResetTokenRepository resetTokens;
    private readonly IMemberDirectory directory;
    private readonly IPasswordHasher passwordHasher;
    private readonly ITemporaryPasswordGenerator passwords;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<MemberAdminHandlers> logger;

    public MemberAdminHandlers(
        AdminGuard guard,
        AdminAudit audit,
        IUserRepository users,
        IBranchRepository branches,
        IRoleAssignmentRepository roleAssignments,
        ITenantMembershipRepository memberships,
        IUserSessionRepository sessions,
        IPasswordResetTokenRepository resetTokens,
        IMemberDirectory directory,
        IPasswordHasher passwordHasher,
        ITemporaryPasswordGenerator passwords,
        IUnitOfWork unitOfWork,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<MemberAdminHandlers> logger)
    {
        this.guard = guard;
        this.audit = audit;
        this.users = users;
        this.branches = branches;
        this.roleAssignments = roleAssignments;
        this.memberships = memberships;
        this.sessions = sessions;
        this.resetTokens = resetTokens;
        this.directory = directory;
        this.passwordHasher = passwordHasher;
        this.passwords = passwords;
        this.unitOfWork = unitOfWork;
        this.localization = localization;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<AddMemberResult> Handle(AddMemberCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        User? existing = await this.users.FindByEmailAsync(EmailAddress.Normalize(request.Email), cancellationToken).ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync(tenantId, actorId, existing?.Id, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            IReadOnlyList<(Role Role, Guid? BranchId)> plan = await RolePlan.BuildAsync(request.Roles, [], this.branches, this.localization, cancellationToken).ConfigureAwait(false);
            DateTimeOffset now = this.timeProvider.GetUtcNow();

            // Sıra içinde yeniden ara (review round 2, L-A): sıra beklerken aynı e-postayla kişi açılmış olabilir (aynı
            // formun iki kez gönderilmesi). Bu okulda ise anlaşılır "zaten kayıtlı"; başka okulda açıldıysa kişinin sırası
            // alınmadığı için "biraz sonra deneyin" — iki durumda da sunucu hatası (500) değil.
            User? user = await this.users.FindByEmailAsync(EmailAddress.Normalize(request.Email), cancellationToken).ConfigureAwait(false);
            if (existing is null && user is not null)
            {
                string key = await this.memberships.FindInCurrentTenantAsync(user.Id, cancellationToken).ConfigureAwait(false) is not null
                    ? AdministrationMessages.MemberAlreadyInTenant
                    : AdministrationMessages.TooManyRequests;
                throw new BusinessException(await this.localization.GetAsync(key).ConfigureAwait(false));
            }

            string? temporaryPassword = null;
            if (user is not null)
            {
                if (await this.memberships.FindInCurrentTenantAsync(user.Id, cancellationToken).ConfigureAwait(false) is not null)
                {
                    throw new BusinessException(await this.localization.GetAsync(AdministrationMessages.MemberAlreadyInTenant).ConfigureAwait(false));
                }
            }
            else
            {
                temporaryPassword = this.passwords.Generate();
                PhoneNumber.TryNormalize(request.Phone, out string? phone);
                user = User.Create(request.Email, request.FullName, this.passwordHasher.Hash(temporaryPassword), now, phone);
                this.users.Add(user);
                this.audit.Record(AuditAction.UserCreated, tenantId, actorId, nameof(User), user.Id);
            }

            this.memberships.Add(TenantMembership.Create(user.Id, tenantId, now));
            this.audit.Record(AuditAction.MemberAdded, tenantId, actorId, nameof(User), user.Id);
            this.AddAssignments(user.Id, tenantId, actorId, plan, now);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("Kişi eklendi: kişi {UserId}, kurum {TenantId}, yeni hesap: {NewAccount}", user.Id, tenantId, temporaryPassword is not null);
            return new AddMemberResult(user.Id, temporaryPassword is null, temporaryPassword);
        }
    }

    public async Task<MemberDetail> Handle(UpdateMemberProfileCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync(tenantId, actorId, request.UserId, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            User user = await this.FindMemberUserAsync(request.UserId, cancellationToken).ConfigureAwait(false);

            // Ad ve telefon kişinin bütün okullarında ortak: başka okulda da kayıtlıysa bir okulun yöneticisi değiştiremez
            // (review M-2; spec 0005 gereksinim 6 — öbür okulun gördüğü bilgi etkilenmez).
            await this.EnsureOnlyThisTenantAsync(user.Id, tenantId, AdministrationMessages.ProfileBlockedOtherTenant, cancellationToken).ConfigureAwait(false);
            PhoneNumber.TryNormalize(request.Phone, out string? phone);
            user.UpdateProfile(request.FullName, phone);
            this.audit.Record(AuditAction.MemberProfileUpdated, tenantId, actorId, nameof(User), user.Id);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        return await this.DetailAsync(request.UserId, actorId, tenantId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<MemberDetail> Handle(SetMemberRolesCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync(tenantId, actorId, null, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            TenantMembership membership = await this.FindMembershipAsync(request.UserId, cancellationToken).ConfigureAwait(false);
            IReadOnlyList<RoleAssignment> current = await this.roleAssignments.ListInCurrentTenantAsync(request.UserId, cancellationToken).ConfigureAwait(false);
            IReadOnlyList<(Role Role, Guid? BranchId)> plan = await RolePlan.BuildAsync(
                request.Roles ?? [], [.. current.Select(a => (a.Role, a.BranchId))], this.branches, this.localization, cancellationToken).ConfigureAwait(false);
            bool wasFounder = current.Any(a => a.Role == Role.Founder);
            bool staysFounder = plan.Any(p => p.Role == Role.Founder);
            if (wasFounder && !staysFounder)
            {
                if (request.UserId == actorId)
                {
                    throw new BusinessException(await this.localization.GetAsync(AdministrationMessages.CannotRemoveOwnFounder).ConfigureAwait(false));
                }

                await this.EnsureAnotherActiveFounderAsync(membership, cancellationToken).ConfigureAwait(false);
            }

            DateTimeOffset now = this.timeProvider.GetUtcNow();
            foreach (RoleAssignment assignment in current.Where(a => !plan.Contains((a.Role, a.BranchId))))
            {
                this.roleAssignments.Remove(assignment);
                this.audit.Record(AuditAction.RoleRevoked, tenantId, actorId, nameof(RoleAssignment), assignment.Id);
            }

            this.AddAssignments(request.UserId, tenantId, actorId, [.. plan.Where(p => !current.Any(a => a.Role == p.Role && a.BranchId == p.BranchId))], now);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        return await this.DetailAsync(request.UserId, actorId, tenantId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<MemberDetail> Handle(SetMemberActiveCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync(tenantId, actorId, null, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            TenantMembership membership = await this.FindMembershipAsync(request.UserId, cancellationToken).ConfigureAwait(false);
            if (request.Active && !membership.IsActive)
            {
                membership.Activate();
                this.audit.Record(AuditAction.MemberActivated, tenantId, actorId, nameof(User), request.UserId);
            }
            else if (!request.Active && membership.IsActive)
            {
                if (request.UserId == actorId)
                {
                    throw new BusinessException(await this.localization.GetAsync(AdministrationMessages.CannotDeactivateSelf).ConfigureAwait(false));
                }

                IReadOnlyList<RoleAssignment> current = await this.roleAssignments.ListInCurrentTenantAsync(request.UserId, cancellationToken).ConfigureAwait(false);
                if (current.Any(a => a.Role == Role.Founder))
                {
                    await this.EnsureAnotherActiveFounderAsync(membership, cancellationToken).ConfigureAwait(false);
                }

                membership.Deactivate();
                this.audit.Record(AuditAction.MemberDeactivated, tenantId, actorId, nameof(User), request.UserId);
            }

            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        return await this.DetailAsync(request.UserId, actorId, tenantId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ResetMemberPasswordResult> Handle(ResetMemberPasswordCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync(tenantId, actorId, request.UserId, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            User user = await this.FindMemberUserAsync(request.UserId, cancellationToken).ConfigureAwait(false);
            await this.EnsureOnlyThisTenantAsync(user.Id, tenantId, AdministrationMessages.ResetBlockedOtherTenant, cancellationToken).ConfigureAwait(false);
            DateTimeOffset now = this.timeProvider.GetUtcNow();
            string temporaryPassword = this.passwords.Generate();
            user.ChangePasswordHash(this.passwordHasher.Hash(temporaryPassword));
            await this.sessions.RevokeAllForUserAsync(user.Id, now, cancellationToken).ConfigureAwait(false);
            await this.resetTokens.RevokeAllForUserAsync(user.Id, now, cancellationToken).ConfigureAwait(false);
            this.audit.Record(AuditAction.PasswordReset, tenantId, actorId, nameof(User), user.Id);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("Şifre sıfırlandı: kişi {UserId}, kurum {TenantId}; açık oturumlar kapatıldı", user.Id, tenantId);
            return new ResetMemberPasswordResult(temporaryPassword);
        }
    }

    public async Task<MemberPage> Handle(ListMembersQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await this.guard.RequireFounderAsync().ConfigureAwait(false);
        Role? role = Enum.TryParse(request.Role, ignoreCase: false, out Role parsed) && Enum.IsDefined(parsed) ? parsed : null;
        bool? active = request.Status switch { "active" => true, "inactive" => false, _ => null };
        MemberListFilter filter = new(string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim(), role, request.BranchId, active, Math.Max(1, request.Page), PageSize);
        return await this.directory.ListAsync(filter, cancellationToken).ConfigureAwait(false);
    }

    public async Task<MemberDetail> Handle(GetMemberQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        return await this.DetailAsync(request.UserId, actorId, tenantId, cancellationToken).ConfigureAwait(false);
    }

    private void AddAssignments(Guid userId, Guid tenantId, Guid actorId, IEnumerable<(Role Role, Guid? BranchId)> plan, DateTimeOffset now)
    {
        foreach ((Role role, Guid? branchId) in plan)
        {
            RoleAssignment assignment = role == Role.Founder
                ? RoleAssignment.Founder(userId, tenantId, now)
                : RoleAssignment.ForBranch(userId, tenantId, role, branchId!.Value, now);
            this.roleAssignments.Add(assignment);
            this.audit.Record(AuditAction.RoleAssigned, tenantId, actorId, nameof(RoleAssignment), assignment.Id);
        }
    }

    private async Task EnsureOnlyThisTenantAsync(Guid userId, Guid tenantId, string messageKey, CancellationToken cancellationToken)
    {
        if (await this.roleAssignments.CountOtherTenantsAsync(userId, tenantId, cancellationToken).ConfigureAwait(false) > 0)
        {
            throw new BusinessException(await this.localization.GetAsync(messageKey).ConfigureAwait(false));
        }
    }

    private async Task EnsureAnotherActiveFounderAsync(TenantMembership membership, CancellationToken cancellationToken)
    {
        int activeFounders = await this.roleAssignments.CountActiveFoundersInCurrentTenantAsync(cancellationToken).ConfigureAwait(false);
        int remaining = membership.IsActive ? activeFounders - 1 : activeFounders;
        if (remaining < 1)
        {
            throw new BusinessException(await this.localization.GetAsync(AdministrationMessages.LastFounder).ConfigureAwait(false));
        }
    }

    private async Task<TenantMembership> FindMembershipAsync(Guid userId, CancellationToken cancellationToken) =>
        await this.memberships.FindInCurrentTenantAsync(userId, cancellationToken).ConfigureAwait(false)
        ?? throw new NotFoundException(await this.localization.GetAsync(AdministrationMessages.MemberNotFound).ConfigureAwait(false));

    private async Task<User> FindMemberUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        await this.FindMembershipAsync(userId, cancellationToken).ConfigureAwait(false);
        return await this.users.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(await this.localization.GetAsync(AdministrationMessages.MemberNotFound).ConfigureAwait(false));
    }

    private async Task<MemberDetail> DetailAsync(Guid userId, Guid actorId, Guid tenantId, CancellationToken cancellationToken)
    {
        MemberRow row = await this.directory.FindAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(await this.localization.GetAsync(AdministrationMessages.MemberNotFound).ConfigureAwait(false));
        bool otherTenant = await this.roleAssignments.CountOtherTenantsAsync(userId, tenantId, cancellationToken).ConfigureAwait(false) > 0;
        return new MemberDetail(row.UserId, row.FullName, row.Email, row.Phone, row.IsActive, row.Roles, userId == actorId, !otherTenant, !otherTenant);
    }
}
