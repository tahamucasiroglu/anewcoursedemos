using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Administration;
using OkulYonetim.Application.Features.Students.Commands.LinkGuardian;
using OkulYonetim.Application.Features.Students.Commands.UnlinkGuardian;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Students;

namespace OkulYonetim.Application.Features.Students;

/// <summary>
/// Veli bağlama ve kaldırma (spec 0009 gereksinim 6–7). Bağlama spec 0005 kurallarını <see cref="MemberAccounts"/> ile
/// yeniden kullanır: sistemde olmayan e-postada hesap açılır ve geçici şifre bir kez döner; var olan kişide hesap açılmaz,
/// şifre ve ad değişmez. Okulda üyeliği yoksa eklenir; pasifse bağlama reddedilir; öğrencinin şubesinde veli rolü yoksa
/// verilir. Aynı veli ikinci kez bağlanamaz, 5. veli reddedilir. Okul + kişi sırası (<see cref="AdminGuard.LockAsync"/>)
/// içinde tek kayıtta yazılır (plan 0009 Risks 3). Bağ kaldırma kişinin hesabına ve rollerine dokunmaz. Audit'li;
/// geçici şifre yalnızca yanıtta.
/// </summary>
public sealed class GuardianLinkHandlers :
    IRequestHandler<LinkGuardianCommand, LinkGuardianResult>,
    IRequestHandler<UnlinkGuardianCommand, StudentSaved>
{
    private readonly AdminGuard guard;
    private readonly AdminAudit audit;
    private readonly MemberAccounts accounts;
    private readonly IStudentRepository students;
    private readonly IStudentGuardianRepository guardians;
    private readonly IUserRepository users;
    private readonly ITenantMembershipRepository memberships;
    private readonly IRoleAssignmentRepository roleAssignments;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<GuardianLinkHandlers> logger;

    public GuardianLinkHandlers(
        AdminGuard guard,
        AdminAudit audit,
        MemberAccounts accounts,
        IStudentRepository students,
        IStudentGuardianRepository guardians,
        IUserRepository users,
        ITenantMembershipRepository memberships,
        IRoleAssignmentRepository roleAssignments,
        IUnitOfWork unitOfWork,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<GuardianLinkHandlers> logger)
    {
        this.guard = guard;
        this.audit = audit;
        this.accounts = accounts;
        this.students = students;
        this.guardians = guardians;
        this.users = users;
        this.memberships = memberships;
        this.roleAssignments = roleAssignments;
        this.unitOfWork = unitOfWork;
        this.localization = localization;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<LinkGuardianResult> Handle(LinkGuardianCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        await this.FindStudentAsync(request.StudentId, cancellationToken).ConfigureAwait(false);
        string email = EmailAddress.Normalize(request.Email);
        User? existing = await this.users.FindByEmailAsync(email, cancellationToken).ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync(tenantId, actorId, existing?.Id, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            Student student = await this.FindStudentAsync(request.StudentId, cancellationToken).ConfigureAwait(false);

            // Sıra içinde yeniden ara (spec 0005 kalıbı): sıra beklerken aynı e-postayla kişi açılmış olabilir; kişinin sırası
            // alınmadığı için "biraz sonra deneyin" — sunucu hatası (500) değil.
            User? user = await this.users.FindByEmailAsync(email, cancellationToken).ConfigureAwait(false);
            if (existing is null && user is not null)
            {
                throw new BusinessException(await this.localization.GetAsync(AdministrationMessages.TooManyRequests).ConfigureAwait(false));
            }

            if (user is not null && await this.guardians.FindAsync(student.Id, user.Id, cancellationToken).ConfigureAwait(false) is not null)
            {
                throw new BusinessException(await this.localization.StudentTextAsync(StudentsMessages.GuardianAlreadyLinked).ConfigureAwait(false));
            }

            if (await this.guardians.CountForStudentAsync(student.Id, cancellationToken).ConfigureAwait(false) >= StudentGuardian.MaxPerStudent)
            {
                throw new BusinessException(await this.localization.StudentTextAsync(StudentsMessages.GuardianLimit).ConfigureAwait(false));
            }

            DateTimeOffset now = this.timeProvider.GetUtcNow();
            string? temporaryPassword = null;
            if (user is null)
            {
                (user, temporaryPassword) = this.accounts.CreateUser(tenantId, actorId, request.Email, request.FullName, request.Phone, now);
                this.accounts.AddMembership(user.Id, tenantId, actorId, now);
            }
            else
            {
                TenantMembership? membership = await this.memberships.FindInCurrentTenantAsync(user.Id, cancellationToken).ConfigureAwait(false);
                if (membership is { IsActive: false })
                {
                    throw new BusinessException(await this.localization.StudentTextAsync(StudentsMessages.GuardianInactive).ConfigureAwait(false));
                }

                if (membership is null)
                {
                    this.accounts.AddMembership(user.Id, tenantId, actorId, now);
                }
            }

            IReadOnlyList<RoleAssignment> roles = temporaryPassword is null
                ? await this.roleAssignments.ListInCurrentTenantAsync(user.Id, cancellationToken).ConfigureAwait(false)
                : [];
            if (!roles.Any(r => r.Role == Role.Guardian && r.BranchId == student.BranchId))
            {
                this.accounts.AddAssignments(user.Id, tenantId, actorId, [(Role.Guardian, student.BranchId)], now);
            }

            GuardianRelationship relationship = LinkGuardianCommandValidator.TryParseRelationship(request.Relationship, out GuardianRelationship parsed)
                ? parsed
                : throw new InvalidOperationException("Yakınlık doğrulayıcıdan geçmeliydi.");
            this.guardians.Add(StudentGuardian.Link(tenantId, student.Id, user.Id, relationship, now));
            this.audit.Record(AuditAction.GuardianLinked, tenantId, actorId, nameof(Student), student.Id);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation(
                "Veli bağlandı: öğrenci {StudentId}, kişi {UserId}, kurum {TenantId}, yeni hesap: {NewAccount}", student.Id, user.Id, tenantId, temporaryPassword is not null);
            return new LinkGuardianResult(user.Id, temporaryPassword is null, temporaryPassword);
        }
    }

    public async Task<StudentSaved> Handle(UnlinkGuardianCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync(tenantId, actorId, null, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            Student student = await this.FindStudentAsync(request.StudentId, cancellationToken).ConfigureAwait(false);
            StudentGuardian link = await this.guardians.FindAsync(student.Id, request.GuardianUserId, cancellationToken).ConfigureAwait(false)
                ?? throw new NotFoundException(await this.localization.StudentTextAsync(StudentsMessages.GuardianNotLinked).ConfigureAwait(false));
            this.guardians.Remove(link);
            this.audit.Record(AuditAction.GuardianUnlinked, tenantId, actorId, nameof(Student), student.Id);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("Veli bağı kaldırıldı: öğrenci {StudentId}, kişi {UserId}", student.Id, request.GuardianUserId);
            return new StudentSaved(student.Id);
        }
    }

    private async Task<Student> FindStudentAsync(Guid id, CancellationToken cancellationToken) =>
        await this.students.FindAsync(id, cancellationToken).ConfigureAwait(false)
        ?? throw new NotFoundException(await this.localization.StudentTextAsync(StudentsMessages.StudentNotFound).ConfigureAwait(false));
}
