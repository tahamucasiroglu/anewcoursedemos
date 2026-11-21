namespace OkulYonetim.Application.Features.Billing;

/// <summary>
/// Tahsilat işlemi yapan personel (spec 0015 C-8): seçili okul, kişi, kurucu mu ve Muhasebe rolü olduğu şubeler (veritabanından, her
/// istekte). Kurucu bütün şubeleri, muhasebe yalnızca kendi Muhasebe şubelerini görür (<see cref="CanSee"/>) — oturumun görünür
/// şubeleri bütün rollerin şubeleri olduğu için yetmez (ör. başka şubede öğretmen olan muhasebe).
/// </summary>
public sealed record BillingStaff(Guid TenantId, Guid UserId, bool IsFounder, IReadOnlySet<Guid> AccountingBranchIds)
{
    public bool CanSee(Guid branchId) => this.IsFounder || this.AccountingBranchIds.Contains(branchId);
}
