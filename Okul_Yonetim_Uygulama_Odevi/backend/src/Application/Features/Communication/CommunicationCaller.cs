namespace OkulYonetim.Application.Features.Communication;

/// <summary>İletişim uçlarını çağıran kişi (spec 0011; her istekte veritabanından): okul, kişi, kurucu mu, Öğretmen rolü olduğu şubeler ve Veli rolü var mı.</summary>
public sealed record CommunicationCaller(Guid TenantId, Guid UserId, bool IsFounder, IReadOnlySet<Guid> TeacherBranchIds, bool IsGuardian)
{
    public bool IsTeacher => this.TeacherBranchIds.Count > 0;
}
