using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Application.Features.Attendance;

/// <summary>
/// Yoklamanın üç katmanlı yetki kapısı (spec 0010 gereksinim 1–2, AC-10; <c>docs/security.md</c> AuthN/AuthZ 4). Her istekte
/// veritabanından: kişinin seçili okulda Öğretmen rolü olmalı (değilse 403 — veli, muhasebe, öğretmen olmayan kurucu).
/// Sınıf kurum + şube filtresinden okunur ve kişi o sınıfa atanmış, sınıfın şubesinde hâlâ Öğretmen ve sınıf etkin olmalı;
/// değilse 404 ("var ama sizin değil" sızdırılmaz). Pasif üyelikte oturum zaten doğrulanmaz (401). Kuralın kendisi ödev/notla
/// ortak <see cref="TeacherClassroomAccess"/>'tadır (plan 0013 Risks 2); burada yalnızca yoklamanın hata metinleri verilir.
/// </summary>
public sealed class AttendanceGuard
{
    private readonly TeacherClassroomAccess access;
    private readonly ILocalizationService localization;

    public AttendanceGuard(TeacherClassroomAccess access, ILocalizationService localization)
    {
        this.access = access;
        this.localization = localization;
    }

    /// <summary>Seçili okul, istek sahibi ve Öğretmen rolü olduğu şubeler; öğretmen değilse 403.</summary>
    public async Task<(Guid TenantId, Guid UserId, IReadOnlySet<Guid> TeacherBranchIds)> RequireTeacherAsync(CancellationToken cancellationToken) =>
        await this.access.TeacherAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new ForbiddenException(await this.localization.AttendanceTextAsync(AttendanceMessages.TeacherOnly).ConfigureAwait(false));

    /// <summary>Öğretmenin bu sınıfa erişimi: görünür, etkin, atanmış ve şubesinde öğretmen; değilse 404.</summary>
    public async Task<ClassroomSummary> RequireClassroomAsync(Guid userId, IReadOnlySet<Guid> teacherBranchIds, Guid classroomId, CancellationToken cancellationToken) =>
        await this.access.AssignedClassroomAsync(userId, teacherBranchIds, classroomId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(await this.localization.AttendanceTextAsync(AttendanceMessages.ClassroomNotFound).ConfigureAwait(false));
}
