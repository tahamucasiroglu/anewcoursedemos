using System.Globalization;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Billing.Ledger;
using OkulYonetim.Application.Features.Billing.Payments;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Features.Students;
using OkulYonetim.Application.Features.Students.Queries.ListStudentLabels;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Billing;

namespace OkulYonetim.Application.Features.Billing.Reports;

/// <summary>
/// Yönetici panelinin tahsilat okumaları (spec 0017; uçsuz, erişimi çağıran denetler). Cari: 0015 cari listesiyle aynı hesap — planlar şube
/// süzgeciyle, taksitler tek sorguda, toplamlar <see cref="BillingLedger.ActiveTotals"/> (gecikme tanımı yalnızca
/// <see cref="Installment.OverdueOn"/>; "bugün" Türkiye günü); en eski geciken vade bugün gecikmiş taksitlerin en küçük vadesi.
/// Tahsilatlar: tahsilat günü aralıkta olanlar, öğrenci etiketi Students'tan.
/// </summary>
public sealed class BillingReportHandlers :
    IRequestHandler<ListStudentBalancesQuery, StudentBalances>,
    IRequestHandler<ListPaymentsInRangeQuery, IReadOnlyList<PaymentRangeRow>>
{
    private static readonly StringComparer TurkishOrder = StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: true);
    private readonly IBillingRepository billing;
    private readonly ISender sender;
    private readonly SchoolCalendar calendar;

    public BillingReportHandlers(IBillingRepository billing, ISender sender, SchoolCalendar calendar)
    {
        this.billing = billing;
        this.sender = sender;
        this.calendar = calendar;
    }

    public async Task<StudentBalances> Handle(ListStudentBalancesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        DateOnly today = this.calendar.Today();
        List<InstallmentPlan> plans = [.. (await this.billing.ListPlansAsync(null, request.BranchId is { } branch ? [branch] : null, cancellationToken).ConfigureAwait(false))
            .Where(p => p.Status == InstallmentPlanStatus.Active)];
        if (plans.Count == 0)
        {
            return new StudentBalances(today, []);
        }

        ILookup<Guid, Installment> installments = (await this.billing.ListInstallmentsAsync([.. plans.Select(p => p.Id)], false, cancellationToken).ConfigureAwait(false))
            .ToLookup(i => i.PlanId);
        Dictionary<Guid, StudentLabel> labels = (await this.sender.Send(new ListStudentLabelsQuery([.. plans.Select(p => p.StudentId).Distinct()]), cancellationToken).ConfigureAwait(false))
            .ToDictionary(l => l.Id);
        List<StudentBalanceRow> rows = [.. plans.GroupBy(p => p.StudentId)
            .Where(g => labels.ContainsKey(g.Key))
            .Select(g =>
            {
                StudentLabel label = labels[g.Key];
                List<Installment> own = [.. g.SelectMany(p => installments[p.Id])];
                DateOnly? oldest = own.Where(i => i.OverdueOn(today) > 0m).Select(i => (DateOnly?)i.DueDate).Min();
                return new StudentBalanceRow(
                    label.Id,
                    label.FullName,
                    label.BranchId,
                    label.BranchName,
                    label.ClassroomName,
                    label.IsActive,
                    [.. g.Select(p => p.FeeName)],
                    BillingLedger.ActiveTotals(g, installments, today),
                    oldest);
            })
            .Where(r => !request.OverdueOnly || r.Totals.Overdue > 0m)
            .OrderBy(r => r.FullName, TurkishOrder).ThenBy(r => r.StudentId)];
        return new StudentBalances(today, rows);
    }

    public async Task<IReadOnlyList<PaymentRangeRow>> Handle(ListPaymentsInRangeQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        IReadOnlyList<PaymentEntry> payments = await this.billing.ListPaymentsInRangeAsync(
            request.FromDay, request.ToDay, request.BranchId is { } branch ? [branch] : null, cancellationToken).ConfigureAwait(false);
        if (payments.Count == 0)
        {
            return [];
        }

        Dictionary<Guid, StudentLabel> labels = (await this.sender.Send(new ListStudentLabelsQuery([.. payments.Select(p => p.Payment.StudentId).Distinct()]), cancellationToken).ConfigureAwait(false))
            .ToDictionary(l => l.Id);
        return [.. payments
            .Where(e => labels.ContainsKey(e.Payment.StudentId))
            .Select(e => new PaymentRangeRow(
                e.Payment.Id,
                e.Payment.Date,
                e.Payment.ReceiptNo,
                e.Payment.StudentId,
                labels[e.Payment.StudentId].FullName,
                labels[e.Payment.StudentId].BranchName,
                e.Payment.Amount,
                e.Payment.Method.ToString(),
                e.Payment.IsCancelled,
                e.RecordedByName))];
    }
}
