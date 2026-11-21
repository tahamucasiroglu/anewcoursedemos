using OkulYonetim.Tests.Integration.Communication;

namespace OkulYonetim.Tests.Integration.Billing;

/// <summary>
/// Spec 0015 testlerinin hazır okulu: 0011'in hazır okulu (Merkez şubesinde 5-A: Ali — annesi ve babası veli —, Ayşe; 5-B: Berk),
/// yalnızca Merkez şubesinde Muhasebe rolü olan kişi, aynı okulun ikinci şubesi (Kadıköy) ve orada annesi veli olan Cem; kurucunun
/// açtığı "2026-2027 Eğitim Ücreti" (10.000,00 TL) tarifesi ve "Kardeş" (%15) indirimi.
/// </summary>
public sealed record BillingWorld(
    CommunicationWorld School,
    Guid AccountantId,
    string AccountantEmail,
    string Accountant,
    Guid OtherBranchId,
    Guid Cem,
    ParentAccount CemMother,
    Guid FeeId,
    Guid DiscountId)
{
    public Guid TenantId => this.School.Class.School.TenantId;

    public Guid BranchId => this.School.Class.School.BranchId;

    public string Founder => this.School.Class.Founder;

    public Guid Ali => this.School.Ali;
}
