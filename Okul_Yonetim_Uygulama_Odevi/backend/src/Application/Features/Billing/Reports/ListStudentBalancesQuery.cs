using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Billing.Reports;

/// <summary>
/// Bugün itibarıyla geçerli planı olan öğrencilerin carisi (spec 0017 gereksinim 4, 9); şube ve "yalnızca gecikenler" süzgeci. İptal plan
/// cariye girmez; yalnızca iptal planı olan öğrenci listede yok; plan adları Türkçe harf sırasıyla. Uç yoktur ve erişim denetlemez (yalnızca kurum + şube filtresi) — çağıran
/// denetler (yönetici paneli kapısı; muhasebe ekranları kendi <c>BillingGuard</c>'ını kullanır).
/// </summary>
public sealed record ListStudentBalancesQuery(Guid? BranchId, bool OverdueOnly) : IRequest<StudentBalances>;
