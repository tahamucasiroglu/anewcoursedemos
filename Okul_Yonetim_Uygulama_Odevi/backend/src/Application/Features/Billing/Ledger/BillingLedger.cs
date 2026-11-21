using OkulYonetim.Domain.Billing;

namespace OkulYonetim.Application.Features.Billing.Ledger;

/// <summary>
/// Cari hesabı (spec 0015 gereksinim 8; saf): plan toplamı = net, kalan = taksit kalanlarının toplamı, ödenen = net − kalan, geciken =
/// taksitlerin bugünkü gecikmiş tutarı (<see cref="Installment.OverdueOn"/> — gecikme tanımı tek yerde). İptal plan toplamları cariye
/// girmez; iptal tahsilat dağılımı zaten geri alınmıştır. Yönetici paneli (spec 0017) bu yardımcıyı kullanır.
/// </summary>
public static class BillingLedger
{
    public static AccountTotals PlanTotals(InstallmentPlan plan, IEnumerable<Installment> installments, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(plan);
        List<Installment> list = [.. installments ?? []];
        decimal remaining = list.Sum(i => i.Remaining);
        return new AccountTotals(plan.NetAmount, plan.NetAmount - remaining, remaining, list.Sum(i => i.OverdueOn(today)));
    }

    /// <summary>Geçerli planların toplamı (iptal planlar dışarıda).</summary>
    public static AccountTotals ActiveTotals(IEnumerable<InstallmentPlan> plans, ILookup<Guid, Installment> installments, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(installments);
        return AccountTotals.Sum((plans ?? []).Where(p => p.Status == InstallmentPlanStatus.Active).Select(p => PlanTotals(p, installments[p.Id], today)));
    }

    public static InstallmentView View(Installment installment, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(installment);
        return new(installment.Sequence, installment.DueDate, installment.Amount, installment.PaidAmount, installment.Remaining, installment.StateOn(today).ToString());
    }
}
