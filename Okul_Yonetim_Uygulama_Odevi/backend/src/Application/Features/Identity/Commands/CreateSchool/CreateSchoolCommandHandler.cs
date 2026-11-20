using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Features.Identity.Commands.CreateSchool;

/// <summary>
/// Okul açma (spec 0004, gereksinim 1/2): e-posta sistemde yoksa kişi verilen şifreyle açılır (şifre zorunlu, en az 8
/// karakter). E-posta varsa yeni kişi açılmaz, şifresi değişmez. Kişi bu adla bir okulun zaten kurucusuysa hiçbir şey
/// değişmez ("zaten var"). Aksi halde okul, ilk şube ve kurucu ataması oluşur; her işlem audit kaydına geçer.
/// </summary>
public sealed class CreateSchoolCommandHandler : IRequestHandler<CreateSchoolCommand, CreateSchoolResult>
{
    private readonly IUserRepository users;
    private readonly ITenantRepository tenants;
    private readonly IBranchRepository branches;
    private readonly IRoleAssignmentRepository roleAssignments;
    private readonly IAuditLog audit;
    private readonly IUnitOfWork unitOfWork;
    private readonly IPasswordHasher passwordHasher;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<CreateSchoolCommandHandler> logger;

    public CreateSchoolCommandHandler(
        IUserRepository users,
        ITenantRepository tenants,
        IBranchRepository branches,
        IRoleAssignmentRepository roleAssignments,
        IAuditLog audit,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<CreateSchoolCommandHandler> logger)
    {
        this.users = users;
        this.tenants = tenants;
        this.branches = branches;
        this.roleAssignments = roleAssignments;
        this.audit = audit;
        this.unitOfWork = unitOfWork;
        this.passwordHasher = passwordHasher;
        this.localization = localization;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<CreateSchoolResult> Handle(CreateSchoolCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        DateTimeOffset now = this.timeProvider.GetUtcNow();
        User? user = await this.users.FindByEmailAsync(EmailAddress.Normalize(request.Email), cancellationToken).ConfigureAwait(false);

        if (user is not null)
        {
            Tenant? existing = await this.tenants.FindFoundedByNameAsync(user.Id, request.SchoolName.Trim(), cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                this.logger.LogInformation("Okul açma: kişi {UserId} kurum {TenantId} için zaten kurucu; değişiklik yok", user.Id, existing.Id);
                return new CreateSchoolResult(true, existing.Id, null, user.Id, false);
            }
        }

        bool userCreated = false;
        if (user is null)
        {
            if (!PasswordPolicy.IsSatisfiedBy(request.Password))
            {
                throw new BusinessException(await this.localization.GetAsync(IdentityMessages.PasswordTooShort).ConfigureAwait(false));
            }

            user = User.Create(request.Email, request.FullName, this.passwordHasher.Hash(request.Password!), now);
            this.users.Add(user);
            userCreated = true;
        }

        Tenant tenant = Tenant.Create(request.SchoolName, now);
        Branch branch = Branch.Create(tenant.Id, request.BranchName, now);
        RoleAssignment founder = RoleAssignment.Founder(user.Id, tenant.Id, now);
        this.tenants.Add(tenant);
        this.branches.Add(branch);
        this.roleAssignments.Add(founder);

        this.audit.Add(AuditEntry.Record(AuditAction.TenantCreated, now, tenant.Id, targetType: nameof(Tenant), targetId: tenant.Id, origin: request.Origin));
        this.audit.Add(AuditEntry.Record(AuditAction.BranchCreated, now, tenant.Id, targetType: nameof(Branch), targetId: branch.Id, origin: request.Origin));
        if (userCreated)
        {
            this.audit.Add(AuditEntry.Record(AuditAction.UserCreated, now, tenant.Id, targetType: nameof(User), targetId: user.Id, origin: request.Origin));
        }

        this.audit.Add(AuditEntry.Record(AuditAction.RoleAssigned, now, tenant.Id, targetType: nameof(RoleAssignment), targetId: founder.Id, origin: request.Origin));
        await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        this.logger.LogInformation("Okul açıldı: kurum {TenantId}, şube {BranchId}, kurucu {UserId} (yeni kişi: {UserCreated})", tenant.Id, branch.Id, user.Id, userCreated);
        return new CreateSchoolResult(false, tenant.Id, branch.Id, user.Id, userCreated);
    }
}
