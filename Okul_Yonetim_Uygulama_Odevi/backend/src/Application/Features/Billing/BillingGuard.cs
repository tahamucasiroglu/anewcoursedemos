using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Students;
using OkulYonetim.Application.Features.Students.Queries.ListStudentLabels;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Features.Billing;

/// <summary>
/// Tahsilatın yetki kapısı (spec 0015 gereksinim 11, C-8; <c>AdminGuard</c> kalıbı). Varsayılan reddet; rol ve şube her istekte
/// veritabanından: personel işlemleri kurucu ya da Muhasebe rolü (yoksa 403 — öğretmen, veli); tarife/indirim yazma yalnızca kurucu
/// (403). Öğrenci, plan, tahsilat ve makbuz başka okulun ya da muhasebenin kendi Muhasebe şubesi dışındaysa 404 (var olduğu
/// sızdırılmaz). Sıra (<see cref="LockAsync"/>) beklemesi aşılırsa "Çok fazla istek".
/// </summary>
public sealed class BillingGuard
{
    private readonly ICurrentSession session;
    private readonly IRoleAssignmentRepository roleAssignments;
    private readonly ISender sender;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILocalizationService localization;

    public BillingGuard(ICurrentSession session, IRoleAssignmentRepository roleAssignments, ISender sender, IUnitOfWork unitOfWork, ILocalizationService localization)
    {
        this.session = session;
        this.roleAssignments = roleAssignments;
        this.sender = sender;
        this.unitOfWork = unitOfWork;
        this.localization = localization;
    }

    /// <summary>Kurucu ya da en az bir şubede Muhasebe; değilse 403.</summary>
    public async Task<BillingStaff> RequireStaffAsync(CancellationToken cancellationToken)
    {
        if (this.session.TenantId is { } tenantId && this.session.UserId is { } userId)
        {
            IReadOnlyList<RoleAssignment> roles = await this.roleAssignments.ListInCurrentTenantAsync(userId, cancellationToken).ConfigureAwait(false);
            bool founder = roles.Any(r => r.Role == Role.Founder);
            HashSet<Guid> accounting = [.. roles.Where(r => r.Role == Role.Accounting && r.BranchId is not null).Select(r => r.BranchId!.Value)];
            if (founder || accounting.Count > 0)
            {
                return new BillingStaff(tenantId, userId, founder, accounting);
            }
        }

        throw new ForbiddenException(await this.localization.BillingTextAsync(BillingMessages.Forbidden).ConfigureAwait(false));
    }

    /// <summary>Tarife ve indirim yazma: yalnızca kurucu (muhasebe dahil diğerleri 403).</summary>
    public async Task<BillingStaff> RequireFounderAsync(CancellationToken cancellationToken)
    {
        BillingStaff staff = await this.RequireStaffAsync(cancellationToken).ConfigureAwait(false);
        return staff.IsFounder
            ? staff
            : throw new ForbiddenException(await this.localization.BillingTextAsync(BillingMessages.FounderOnly).ConfigureAwait(false));
    }

    /// <summary>Öğrenci görünür ve personelin şubesinde; değilse 404 ("Öğrenci bulunamadı").</summary>
    public async Task<StudentLabel> RequireStudentAsync(BillingStaff staff, Guid studentId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(staff);
        IReadOnlyList<StudentLabel> labels = await this.sender.Send(new ListStudentLabelsQuery([studentId]), cancellationToken).ConfigureAwait(false);
        StudentLabel? label = labels.Count > 0 ? labels[0] : null;
        return label is not null && staff.CanSee(label.BranchId)
            ? label
            : throw await this.NotFoundAsync(BillingMessages.StudentNotFound).ConfigureAwait(false);
    }

    /// <summary>Kaydın şubesi personelin şubesi mi; değilse kaydın kendi "bulunamadı" metniyle 404.</summary>
    public async Task EnsureBranchAsync(BillingStaff staff, Guid branchId, string notFoundKey)
    {
        ArgumentNullException.ThrowIfNull(staff);
        if (!staff.CanSee(branchId))
        {
            throw await this.NotFoundAsync(notFoundKey).ConfigureAwait(false);
        }
    }

    /// <summary>Anahtarlarla sıraya girer (her zaman aynı sırayla); bekleme aşılırsa 400 "Çok fazla istek".</summary>
    public async Task<IExclusiveScope> LockAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken)
    {
        try
        {
            return await this.unitOfWork.BeginExclusiveAsync(keys, cancellationToken).ConfigureAwait(false);
        }
        catch (ExclusiveLockTimeoutException)
        {
            throw new BusinessException(await this.localization.BillingTextAsync(BillingMessages.TooManyRequests).ConfigureAwait(false));
        }
    }

    public async Task<NotFoundException> NotFoundAsync(string key) =>
        new(await this.localization.BillingTextAsync(key).ConfigureAwait(false));

    public async Task<BusinessException> RuleAsync(string key) =>
        new(await this.localization.BillingTextAsync(key).ConfigureAwait(false));
}
