namespace OkulYonetim.Application.Features.Billing.Ledger;

/// <summary>Velinin gördüğü çocuk carisi (spec 0015): ad, şube, sınıf, toplamlar ve geçerli planlar (yoksa boş — "Henüz ödeme planı yok").</summary>
public sealed record ChildAccount(Guid StudentId, string FullName, string BranchName, string? ClassroomName, AccountTotals Totals, IReadOnlyList<ChildPlan> Plans);
