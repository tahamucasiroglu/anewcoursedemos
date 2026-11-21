using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Features.Students;
using OkulYonetim.Application.Features.Students.Queries.ListStudentBriefs;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Application.Features.Ai.WeeklySummaries;

/// <summary>
/// Haftalık özetin yetki kapısı (spec 0014 gereksinim 6, AC-9; <c>docs/security.md</c> AuthN/AuthZ 4). Her istekte
/// veritabanından, yoklama ve ödevle aynı kuralla (<see cref="TeacherClassroomAccess"/>): seçili okulda Öğretmen rolü yoksa 403
/// (veli, muhasebe, öğretmen olmayan kurucu); sınıf görünmüyor, pasif, kişi atanmamış ya da sınıfın şubesinde artık öğretmen
/// değilse 404. Öğrenci üzerinden gelen istekte öğrenci görünür ve ayrılmamış olmalı (Students'ın kısa bilgi sorgusu — kurum + şube
/// filtresi), bugün bir sınıfa kayıtlı olmalı ve öğretmen o sınıfa erişebilmeli; değilse "Öğrenci bulunamadı" (404 — başka sınıfın
/// ya da başka okulun öğrencisi sızdırılmaz). Öğrenci bazlı okuma sorguları (yoklama, ödev/not) erişim denetlemediği için dayanak
/// yalnızca <see cref="RequireStudentAsync"/>'ten geçen öğrenci için toplanır (plan 0014 Risks 9).
/// </summary>
public sealed class WeeklySummaryGuard
{
    private readonly TeacherClassroomAccess access;
    private readonly ISender sender;
    private readonly ILocalizationService localization;

    public WeeklySummaryGuard(TeacherClassroomAccess access, ISender sender, ILocalizationService localization)
    {
        this.access = access;
        this.sender = sender;
        this.localization = localization;
    }

    /// <summary>Seçili okul, istek sahibi ve Öğretmen rolü olduğu şubeler; öğretmen değilse 403.</summary>
    public async Task<(Guid TenantId, Guid UserId, IReadOnlySet<Guid> TeacherBranchIds)> RequireTeacherAsync(CancellationToken cancellationToken) =>
        await this.access.TeacherAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new ForbiddenException(await this.localization.AiTextAsync(AiMessages.SummaryTeacherOnly).ConfigureAwait(false));

    /// <summary>Öğretmenin bu sınıfa erişimi; değilse 404 ("Sınıf bulunamadı").</summary>
    public async Task<ClassroomSummary> RequireClassroomAsync(Guid userId, IReadOnlySet<Guid> teacherBranchIds, Guid classroomId, CancellationToken cancellationToken) =>
        await this.AssignedAsync(userId, teacherBranchIds, classroomId, cancellationToken).ConfigureAwait(false)
            ?? throw await this.NotFoundAsync(AiMessages.SummaryClassroomNotFound).ConfigureAwait(false);

    /// <summary>Öğretmenin bu öğrenciye erişimi: öğrencinin bugünkü sınıfına atanmış olmalı; değilse 404 ("Öğrenci bulunamadı"), rol yoksa 403.</summary>
    public async Task<WeeklySummaryStudent> RequireStudentAsync(Guid studentId, CancellationToken cancellationToken)
    {
        (Guid tenantId, Guid userId, IReadOnlySet<Guid> branchIds) = await this.RequireTeacherAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<StudentBrief> briefs = await this.sender.Send(new ListStudentBriefsQuery(Ids: [studentId]), cancellationToken).ConfigureAwait(false);
        StudentBrief? student = briefs.SingleOrDefault(s => s.Id == studentId);
        ClassroomSummary? classroom = student?.ClassroomId is { } classroomId
            ? await this.AssignedAsync(userId, branchIds, classroomId, cancellationToken).ConfigureAwait(false)
            : null;
        return student is not null && classroom is not null
            ? new WeeklySummaryStudent(tenantId, userId, student.Id, student.FullName, classroom.Id)
            : throw await this.NotFoundAsync(AiMessages.SummaryStudentNotFound).ConfigureAwait(false);
    }

    public async Task<NotFoundException> NotFoundAsync(string key) =>
        new(await this.localization.AiTextAsync(key).ConfigureAwait(false));

    private async Task<ClassroomSummary?> AssignedAsync(Guid userId, IReadOnlySet<Guid> teacherBranchIds, Guid classroomId, CancellationToken cancellationToken)
    {
        try
        {
            return await this.access.AssignedClassroomAsync(userId, teacherBranchIds, classroomId, cancellationToken).ConfigureAwait(false);
        }
        catch (NotFoundException)
        {
            return null;
        }
    }
}
