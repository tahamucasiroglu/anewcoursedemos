using System.Globalization;
using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Administration;
using OkulYonetim.Application.Features.Students.Commands.CreateStudent;
using OkulYonetim.Application.Features.Students.Commands.SetPickupPersons;
using OkulYonetim.Application.Features.Students.Commands.SetStudentActive;
using OkulYonetim.Application.Features.Students.Commands.SetStudentHealth;
using OkulYonetim.Application.Features.Students.Commands.UpdateStudent;
using OkulYonetim.Application.Features.Students.Queries.GetStudent;
using OkulYonetim.Application.Features.Students.Queries.ListStudents;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;
using OkulYonetim.Domain.Students;

namespace OkulYonetim.Application.Features.Students;

/// <summary>
/// Kurucunun öğrenci yönetimi (spec 0009 gereksinim 1, 3–5, 9, 10, 12): ekleme, düzenleme (şube değişmez; sınıf aynı
/// şubenin etkin sınıfı ya da boş), sağlık bölümü, ayrıldı/geri al, teslim alabilecek kişiler, liste ve kart. Yalnızca
/// seçili okulun kurucusu (<see cref="AdminGuard"/>); başka okulun öğrencisi "bulunamadı". Değişiklikler okul başına
/// sıralıdır (okul numarası tekilliği eşzamanlı isteklerle atlatılamaz). Her değişiklik ve her kart okunuşu audit'e
/// geçer; loglara yalnızca kimlikler yazılır (ad ve sağlık metni asla).
/// </summary>
public sealed class StudentAdminHandlers :
    IRequestHandler<CreateStudentCommand, StudentSaved>,
    IRequestHandler<UpdateStudentCommand, StudentSaved>,
    IRequestHandler<SetStudentHealthCommand, StudentSaved>,
    IRequestHandler<SetStudentActiveCommand, StudentSaved>,
    IRequestHandler<SetPickupPersonsCommand, StudentSaved>,
    IRequestHandler<ListStudentsQuery, StudentPage>,
    IRequestHandler<GetStudentQuery, StudentDetail>
{
    public const int PageSize = 25;

    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly StringComparer TurkishOrder = StringComparer.Create(Turkish, ignoreCase: true);

    private readonly AdminGuard guard;
    private readonly AdminAudit audit;
    private readonly IStudentRepository students;
    private readonly IStudentGuardianRepository guardians;
    private readonly IBranchRepository branches;
    private readonly IClassroomRepository classrooms;
    private readonly StudentHealth health;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<StudentAdminHandlers> logger;

    public StudentAdminHandlers(
        AdminGuard guard,
        AdminAudit audit,
        IStudentRepository students,
        IStudentGuardianRepository guardians,
        IBranchRepository branches,
        IClassroomRepository classrooms,
        StudentHealth health,
        IUnitOfWork unitOfWork,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<StudentAdminHandlers> logger)
    {
        this.guard = guard;
        this.audit = audit;
        this.students = students;
        this.guardians = guardians;
        this.branches = branches;
        this.classrooms = classrooms;
        this.health = health;
        this.unitOfWork = unitOfWork;
        this.localization = localization;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<StudentSaved> Handle(CreateStudentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync(tenantId, actorId, null, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            Branch? branch = await this.branches.FindVisibleAsync(request.BranchId!.Value, cancellationToken).ConfigureAwait(false);
            if (branch is not { IsActive: true })
            {
                throw new BusinessException(await this.localization.StudentTextAsync(StudentsMessages.BranchUnavailable).ConfigureAwait(false));
            }

            if (request.ClassroomId is { } classroomId)
            {
                await this.EnsureClassroomAsync(classroomId, branch.Id, cancellationToken).ConfigureAwait(false);
            }

            await this.EnsureSchoolNumberFreeAsync(request.SchoolNumber, null, cancellationToken).ConfigureAwait(false);
            (string? noteCipher, string? allergyCipher) = await this.health.ProtectAsync(request.HealthNote, request.Allergy, null).ConfigureAwait(false);
            DateTimeOffset now = this.timeProvider.GetUtcNow();
            Student student = Student.Create(tenantId, branch.Id, request.ClassroomId, request.FullName, request.BirthDate!.Value, request.SchoolNumber, now, actorId);
            if (noteCipher is not null || allergyCipher is not null)
            {
                student.SetHealth(noteCipher, allergyCipher, now, actorId);
            }

            this.students.Add(student);
            this.audit.Record(AuditAction.StudentCreated, tenantId, actorId, nameof(Student), student.Id);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("Öğrenci kaydedildi: {StudentId}, kurum {TenantId}, şube {BranchId}", student.Id, tenantId, branch.Id);
            return new StudentSaved(student.Id);
        }
    }

    public async Task<StudentSaved> Handle(UpdateStudentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync(tenantId, actorId, null, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            Student student = await this.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
            if (request.BranchId is { } branchId && branchId != student.BranchId)
            {
                throw new BusinessException(await this.localization.StudentTextAsync(StudentsMessages.BranchCannotChange).ConfigureAwait(false));
            }

            // Pasif sınıf yeni seçilemez; öğrencinin mevcut (sonradan pasifleşmiş) sınıfı ise korunabilir.
            if (request.ClassroomId is { } classroomId && classroomId != student.ClassroomId)
            {
                await this.EnsureClassroomAsync(classroomId, student.BranchId, cancellationToken).ConfigureAwait(false);
            }

            await this.EnsureSchoolNumberFreeAsync(request.SchoolNumber, student.Id, cancellationToken).ConfigureAwait(false);
            DateTimeOffset now = this.timeProvider.GetUtcNow();
            student.UpdateDetails(request.FullName, request.BirthDate!.Value, request.SchoolNumber, now, actorId);
            student.ChangeClassroom(request.ClassroomId, now, actorId);
            this.audit.Record(AuditAction.StudentUpdated, tenantId, actorId, nameof(Student), student.Id);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("Öğrenci düzenlendi: {StudentId}", student.Id);
            return new StudentSaved(student.Id);
        }
    }

    public async Task<StudentSaved> Handle(SetStudentHealthCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync(tenantId, actorId, null, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            Student student = await this.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
            (string? noteCipher, string? allergyCipher) = await this.health.ProtectAsync(request.HealthNote, request.Allergy, student.Id, StudentHealth.HasAny(student)).ConfigureAwait(false);
            student.SetHealth(noteCipher, allergyCipher, this.timeProvider.GetUtcNow(), actorId);
            this.audit.Record(AuditAction.StudentHealthUpdated, tenantId, actorId, nameof(Student), student.Id);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("Öğrenci sağlık bilgisi kaydedildi: {StudentId}", student.Id);
            return new StudentSaved(student.Id);
        }
    }

    public async Task<StudentSaved> Handle(SetStudentActiveCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync(tenantId, actorId, null, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            Student student = await this.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
            DateTimeOffset now = this.timeProvider.GetUtcNow();
            if (request.Active && !student.IsActive)
            {
                student.Reinstate(now, actorId);
                this.audit.Record(AuditAction.StudentReinstated, tenantId, actorId, nameof(Student), student.Id);
            }
            else if (!request.Active && student.IsActive)
            {
                student.MarkLeft(now, actorId);
                this.audit.Record(AuditAction.StudentLeft, tenantId, actorId, nameof(Student), student.Id);
            }

            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("Öğrenci durumu: {StudentId} etkin={Active}", student.Id, student.IsActive);
            return new StudentSaved(student.Id);
        }
    }

    public async Task<StudentSaved> Handle(SetPickupPersonsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync(tenantId, actorId, null, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            Student student = await this.FindAsync(request.StudentId, cancellationToken).ConfigureAwait(false);
            foreach (PickupPerson existing in await this.students.ListPickupPersonsAsync(student.Id, cancellationToken).ConfigureAwait(false))
            {
                this.students.RemovePickupPerson(existing);
            }

            DateTimeOffset now = this.timeProvider.GetUtcNow();
            int position = 0;
            foreach (PickupPersonInput person in request.Persons ?? [])
            {
                PhoneNumber.TryNormalize(person.Phone, out string? phone);
                this.students.AddPickupPerson(PickupPerson.Create(tenantId, student.Id, ++position, person.FullName, person.Relationship, phone!, now));
            }

            this.audit.Record(AuditAction.PickupPersonsChanged, tenantId, actorId, nameof(Student), student.Id);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("Teslim alabilecek kişiler kaydedildi: öğrenci {StudentId}, {Count} kişi", student.Id, position);
            return new StudentSaved(student.Id);
        }
    }

    public async Task<StudentPage> Handle(ListStudentsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await this.guard.RequireFounderAsync().ConfigureAwait(false);
        string? search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        bool? active = request.Status switch { "active" => true, "left" => false, _ => null };
        int page = Math.Max(1, request.Page);

        // Arama ve sıralama Türkçe harf kuralıyla uygulamada (veritabanı yerel ayarı "İ/i" eşlemeyebilir); okul başına
        // öğrenci sayısı birkaç yüz-bin mertebesinde ve satırlar sağlık alanı olmadan hafif bir projeksiyon.
        List<StudentRow> matched = [.. (await this.students.ListRowsAsync(cancellationToken).ConfigureAwait(false))
            .Where(r => request.BranchId is null || r.BranchId == request.BranchId)
            .Where(r => request.ClassroomId is null || r.ClassroomId == request.ClassroomId)
            .Where(r => active is null || r.IsActive == active)
            .Where(r => search is null
                || Turkish.CompareInfo.IndexOf(r.FullName, search, CompareOptions.IgnoreCase) >= 0
                || (r.SchoolNumber is not null && r.SchoolNumber.Contains(search, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(r => r.FullName, TurkishOrder)
            .ThenBy(r => r.Id)];
        return new StudentPage([.. matched.Skip((page - 1) * PageSize).Take(PageSize)], matched.Count, page, PageSize);
    }

    public async Task<StudentDetail> Handle(GetStudentQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        Student student = await this.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        Branch branch = await this.branches.FindVisibleAsync(student.BranchId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Öğrencinin şubesi okunamadı.");
        Classroom? classroom = student.ClassroomId is { } classroomId
            ? await this.classrooms.FindAsync(classroomId, cancellationToken).ConfigureAwait(false)
            : null;
        StudentHealthView healthView = this.health.Read(student);
        IReadOnlyList<StudentGuardianView> guardianViews = await this.guardians.ListGuardiansAsync(student.Id, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<SiblingRow> siblings = [.. (await this.guardians.ListSiblingsAsync(student.Id, cancellationToken).ConfigureAwait(false))
            .OrderBy(s => s.FullName, TurkishOrder).ThenBy(s => s.Id)];
        IReadOnlyList<PickupPersonView> pickups = [.. (await this.students.ListPickupPersonsAsync(student.Id, cancellationToken).ConfigureAwait(false))
            .Select(p => new PickupPersonView(p.FullName, p.Relationship, p.Phone))];

        this.audit.Record(AuditAction.StudentViewed, tenantId, actorId, nameof(Student), student.Id);
        if (healthView.Available && StudentHealth.HasAny(student))
        {
            this.audit.Record(AuditAction.StudentHealthRead, tenantId, actorId, nameof(Student), student.Id);
        }

        await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new StudentDetail(
            student.Id,
            student.FullName,
            student.BirthDate,
            student.SchoolNumber,
            branch.Id,
            branch.Name,
            student.ClassroomId,
            classroom?.Name,
            student.IsActive,
            healthView,
            guardianViews,
            siblings,
            pickups);
    }

    private async Task<Student> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await this.students.FindAsync(id, cancellationToken).ConfigureAwait(false)
        ?? throw new NotFoundException(await this.localization.StudentTextAsync(StudentsMessages.StudentNotFound).ConfigureAwait(false));

    private async Task EnsureClassroomAsync(Guid classroomId, Guid branchId, CancellationToken cancellationToken)
    {
        Classroom? classroom = await this.classrooms.FindAsync(classroomId, cancellationToken).ConfigureAwait(false);
        bool usable = classroom is not null && classroom.BranchId == branchId && classroom.IsActive;
        if (!usable)
        {
            throw new BusinessException(await this.localization.StudentTextAsync(StudentsMessages.ClassroomUnavailable).ConfigureAwait(false));
        }
    }

    private async Task EnsureSchoolNumberFreeAsync(string? schoolNumber, Guid? exceptId, CancellationToken cancellationToken)
    {
        if (Student.NormalizeSchoolNumber(schoolNumber) is { } key
            && await this.students.SchoolNumberExistsAsync(key, exceptId, cancellationToken).ConfigureAwait(false))
        {
            throw new BusinessException(await this.localization.StudentTextAsync(StudentsMessages.SchoolNumberExists).ConfigureAwait(false));
        }
    }
}
