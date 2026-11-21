using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Application.Features.Academics;

/// <summary>
/// Ödev ve notun yetki kapısı (spec 0013 C-4, gereksinim 9; <c>docs/security.md</c> AuthN/AuthZ 4). Her istekte veritabanından,
/// yoklamayla aynı kuralla (<see cref="TeacherClassroomAccess"/>): Öğretmen rolü yoksa 403 (veli, muhasebe, öğretmen olmayan
/// kurucu); sınıf görünmüyor, pasif, kişi atanmamış ya da sınıfın şubesinde artık öğretmen değilse 404. Ödev ya da
/// değerlendirme üzerinden gelen istekte de aynı: kaydın sınıfına erişim yoksa kayıt "bulunamadı". Teslim durumunu ve puanı
/// yalnızca kaydı oluşturan öğretmen değiştirir (<see cref="EnsureOwnerAsync"/>; değilse 403) — oluşturan sınıftan alınmışsa
/// sınıf denetimi önce 404 verir, kimse değiştiremez.
/// </summary>
public sealed class AcademicsGuard
{
    private readonly TeacherClassroomAccess access;
    private readonly ILocalizationService localization;

    public AcademicsGuard(TeacherClassroomAccess access, ILocalizationService localization)
    {
        this.access = access;
        this.localization = localization;
    }

    /// <summary>Seçili okul, istek sahibi ve Öğretmen rolü olduğu şubeler; öğretmen değilse 403.</summary>
    public async Task<(Guid TenantId, Guid UserId, IReadOnlySet<Guid> TeacherBranchIds)> RequireTeacherAsync(CancellationToken cancellationToken) =>
        await this.access.TeacherAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new ForbiddenException(await this.localization.AcademicsTextAsync(AcademicsMessages.TeacherOnly).ConfigureAwait(false));

    /// <summary>Öğretmenin bu sınıfa erişimi; değilse 404 ("Sınıf bulunamadı").</summary>
    public Task<ClassroomSummary> RequireClassroomAsync(Guid userId, IReadOnlySet<Guid> teacherBranchIds, Guid classroomId, CancellationToken cancellationToken) =>
        this.RequireAsync(userId, teacherBranchIds, classroomId, AcademicsMessages.ClassroomNotFound, cancellationToken);

    /// <summary>Ödevin ya da değerlendirmenin sınıfına erişim; değilse kaydın kendi "bulunamadı" metniyle 404.</summary>
    public Task<ClassroomSummary> RequireRecordClassroomAsync(Guid userId, IReadOnlySet<Guid> teacherBranchIds, Guid classroomId, string notFoundKey, CancellationToken cancellationToken) =>
        this.RequireAsync(userId, teacherBranchIds, classroomId, notFoundKey, cancellationToken);

    /// <summary>Kaydı değiştirmek isteyen, kaydı oluşturan öğretmen mi; değilse 403.</summary>
    public async Task EnsureOwnerAsync(Guid createdByUserId, Guid userId)
    {
        if (createdByUserId != userId)
        {
            throw new ForbiddenException(await this.localization.AcademicsTextAsync(AcademicsMessages.OwnerOnly).ConfigureAwait(false));
        }
    }

    public async Task<NotFoundException> NotFoundAsync(string key) =>
        new(await this.localization.AcademicsTextAsync(key).ConfigureAwait(false));

    private async Task<ClassroomSummary> RequireAsync(Guid userId, IReadOnlySet<Guid> teacherBranchIds, Guid classroomId, string notFoundKey, CancellationToken cancellationToken)
    {
        ClassroomSummary? classroom;
        try
        {
            classroom = await this.access.AssignedClassroomAsync(userId, teacherBranchIds, classroomId, cancellationToken).ConfigureAwait(false);
        }
        catch (NotFoundException)
        {
            classroom = null;
        }

        return classroom ?? throw await this.NotFoundAsync(notFoundKey).ConfigureAwait(false);
    }
}
