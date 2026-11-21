using System.Globalization;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Billing.Payments;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Features.Organization.Queries.GetBranches;
using OkulYonetim.Application.Features.Students;
using OkulYonetim.Application.Features.Students.Queries.ListMyChildren;
using OkulYonetim.Application.Features.Students.Queries.ListStudentLabels;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Billing;

namespace OkulYonetim.Application.Features.Billing.Ledger;

/// <summary>
/// Cari okumaları (spec 0015 gereksinim 8, 10). Personel (<see cref="BillingGuard"/>): kurucu bütün şubelerin, muhasebe yalnızca kendi
/// Muhasebe şubelerinin planı olan öğrencilerini görür; başka şubenin öğrencisinin carisi 404. Liste: şube süzgeci (erişilemeyen şube
/// boş sonuç), "yalnızca gecikenler", ad araması (Türkçe harf kuralı), Türkçe ad sırası, 25'lik sayfa; toplamlar süzülmüş bütün
/// satırların. Veli: seçili okulda bağlı ve ayrılmamış çocukları ("Çocuklarım" sorgusu — veli değilse 403), yalnızca geçerli planlar;
/// not, iptal gerekçesi ve kaydeden kişi veliye gösterilmez. "Bugün" Türkiye günü (<see cref="SchoolCalendar"/>).
/// </summary>
public sealed class LedgerHandlers :
    IRequestHandler<ListStudentAccountsQuery, StudentAccountPage>,
    IRequestHandler<GetStudentAccountQuery, StudentAccount>,
    IRequestHandler<ListMyChildrenAccountsQuery, MyPayments>,
    IRequestHandler<GetMyChildAccountQuery, ChildAccount>
{
    public const int PageSize = 25;

    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly StringComparer TurkishOrder = StringComparer.Create(Turkish, ignoreCase: true);

    private readonly BillingGuard guard;
    private readonly IBillingRepository billing;
    private readonly ISender sender;
    private readonly SchoolCalendar calendar;

    public LedgerHandlers(BillingGuard guard, IBillingRepository billing, ISender sender, SchoolCalendar calendar)
    {
        this.guard = guard;
        this.billing = billing;
        this.sender = sender;
        this.calendar = calendar;
    }

    public async Task<StudentAccountPage> Handle(ListStudentAccountsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        BillingStaff staff = await this.guard.RequireStaffAsync(cancellationToken).ConfigureAwait(false);
        List<BranchSummary> branches = [.. (await this.sender.Send(new GetBranchesQuery(), cancellationToken).ConfigureAwait(false)).Where(b => staff.CanSee(b.Id))];
        IReadOnlyCollection<Guid>? branchFilter = request.BranchId is { } branchId
            ? (staff.CanSee(branchId) ? [branchId] : [])
            : (staff.IsFounder ? null : staff.AccountingBranchIds);
        IReadOnlyList<InstallmentPlan> plans = branchFilter is { Count: 0 } ? [] : await this.billing.ListPlansAsync(null, branchFilter, cancellationToken).ConfigureAwait(false);
        ILookup<Guid, Installment> installments = await this.InstallmentsAsync(plans, cancellationToken).ConfigureAwait(false);
        DateOnly today = this.calendar.Today();
        Dictionary<Guid, StudentLabel> labels = (await this.sender.Send(new ListStudentLabelsQuery([.. plans.Select(p => p.StudentId).Distinct()]), cancellationToken).ConfigureAwait(false))
            .ToDictionary(l => l.Id);
        string? search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        List<StudentAccountRow> rows = [.. plans.GroupBy(p => p.StudentId)
            .Where(g => labels.ContainsKey(g.Key) && staff.CanSee(labels[g.Key].BranchId))
            .Select(g => (Label: labels[g.Key], Totals: BillingLedger.ActiveTotals(g, installments, today)))
            .Where(x => search is null || Turkish.CompareInfo.IndexOf(x.Label.FullName, search, CompareOptions.IgnoreCase) >= 0)
            .Where(x => !request.OverdueOnly || x.Totals.Overdue > 0m)
            .OrderBy(x => x.Label.FullName, TurkishOrder).ThenBy(x => x.Label.Id)
            .Select(x => new StudentAccountRow(x.Label.Id, x.Label.FullName, x.Label.BranchName, x.Label.ClassroomName, x.Label.IsActive, x.Totals))];
        return new StudentAccountPage(
            [.. rows.Skip((request.Page - 1) * PageSize).Take(PageSize)],
            request.Page,
            PageSize,
            rows.Count,
            AccountTotals.Sum(rows.Select(r => r.Totals)),
            [.. branches.OrderBy(b => b.Name, TurkishOrder)]);
    }

    public async Task<StudentAccount> Handle(GetStudentAccountQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        BillingStaff staff = await this.guard.RequireStaffAsync(cancellationToken).ConfigureAwait(false);
        StudentLabel student = await this.guard.RequireStudentAsync(staff, request.StudentId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<InstallmentPlan> plans = [.. (await this.billing.ListPlansAsync([student.Id], null, cancellationToken).ConfigureAwait(false)).Where(p => staff.CanSee(p.BranchId))];
        ILookup<Guid, Installment> installments = await this.InstallmentsAsync(plans, cancellationToken).ConfigureAwait(false);
        Dictionary<Guid, InstallmentPlan> byId = plans.ToDictionary(p => p.Id);
        IReadOnlyList<PaymentEntry> payments = plans.Count == 0 ? [] : await this.billing.ListPaymentsAsync([.. byId.Keys], cancellationToken).ConfigureAwait(false);
        DateOnly today = this.calendar.Today();
        return new StudentAccount(
            student.Id,
            student.FullName,
            student.BranchName,
            student.ClassroomName,
            student.IsActive,
            BillingLedger.ActiveTotals(plans, installments, today),
            [.. plans.Select(p => new PlanView(
                p.Id,
                p.FeeName,
                p.FeeAmount,
                p.DiscountName,
                p.DiscountPercent,
                p.DiscountAmount,
                p.InstallmentCount,
                p.Status.ToString(),
                p.CreatedAt,
                p.CancelledAt,
                BillingLedger.PlanTotals(p, installments[p.Id], today),
                [.. installments[p.Id].OrderBy(i => i.Sequence).Select(i => BillingLedger.View(i, today))]))],
            [.. payments.Select(e => new PaymentRow(
                e.Payment.Id,
                e.Payment.PlanId,
                byId[e.Payment.PlanId].FeeName,
                e.Payment.ReceiptNo,
                e.Payment.Date,
                e.Payment.Amount,
                e.Payment.Method.ToString(),
                e.Payment.Note,
                e.RecordedByName,
                e.Payment.IsCancelled,
                e.Payment.CancelledAt,
                e.Payment.CancelReason))],
            this.Movements(plans, payments));
    }

    public async Task<MyPayments> Handle(ListMyChildrenAccountsQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<ChildRow> children = await this.sender.Send(new ListMyChildrenQuery(), cancellationToken).ConfigureAwait(false);
        List<ChildAccount> accounts = await this.ChildAccountsAsync(children, cancellationToken).ConfigureAwait(false);
        return new MyPayments(accounts.Sum(a => a.Totals.Remaining), accounts);
    }

    public async Task<ChildAccount> Handle(GetMyChildAccountQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        List<ChildRow> child = [.. (await this.sender.Send(new ListMyChildrenQuery(), cancellationToken).ConfigureAwait(false)).Where(c => c.Id == request.StudentId)];
        return child.Count == 1
            ? (await this.ChildAccountsAsync(child, cancellationToken).ConfigureAwait(false))[0]
            : throw await this.guard.NotFoundAsync(BillingMessages.StudentNotFound).ConfigureAwait(false);
    }

    /// <summary>Velinin çocuklarının carisi: yalnızca geçerli planlar; not, gerekçe ve kaydeden yok.</summary>
    private async Task<List<ChildAccount>> ChildAccountsAsync(IReadOnlyList<ChildRow> children, CancellationToken cancellationToken)
    {
        if (children.Count == 0)
        {
            return [];
        }

        IReadOnlyList<InstallmentPlan> plans = [.. (await this.billing.ListPlansAsync([.. children.Select(c => c.Id)], null, cancellationToken).ConfigureAwait(false))
            .Where(p => p.Status == InstallmentPlanStatus.Active)];
        ILookup<Guid, Installment> installments = await this.InstallmentsAsync(plans, cancellationToken).ConfigureAwait(false);
        ILookup<Guid, PaymentEntry> payments = (plans.Count == 0 ? [] : await this.billing.ListPaymentsAsync([.. plans.Select(p => p.Id)], cancellationToken).ConfigureAwait(false))
            .ToLookup(e => e.Payment.PlanId);
        DateOnly today = this.calendar.Today();
        return [.. children.Select(c =>
        {
            List<InstallmentPlan> mine = [.. plans.Where(p => p.StudentId == c.Id)];
            return new ChildAccount(
                c.Id,
                c.FullName,
                c.BranchName,
                c.ClassroomName,
                BillingLedger.ActiveTotals(mine, installments, today),
                [.. mine.Select(p => new ChildPlan(
                    p.Id,
                    p.FeeName,
                    BillingLedger.PlanTotals(p, installments[p.Id], today),
                    [.. installments[p.Id].OrderBy(i => i.Sequence).Select(i => BillingLedger.View(i, today))],
                    [.. payments[p.Id].Select(e => new ChildPayment(e.Payment.Date, e.Payment.Amount, e.Payment.Method.ToString(), e.Payment.ReceiptNo, e.Payment.IsCancelled))]))]);
        })];
    }

    private async Task<ILookup<Guid, Installment>> InstallmentsAsync(IReadOnlyList<InstallmentPlan> plans, CancellationToken cancellationToken) =>
        (plans.Count == 0 ? [] : await this.billing.ListInstallmentsAsync([.. plans.Select(p => p.Id)], false, cancellationToken).ConfigureAwait(false))
            .ToLookup(i => i.PlanId);

    /// <summary>Geçerli planların borcu (açılış günü, Türkiye) ve geçerli tahsilatlar (tahsilat günü); tarih, sonra borç önce, sonra makbuz sırasıyla; yürüyen bakiye.</summary>
    private List<LedgerMovement> Movements(IReadOnlyList<InstallmentPlan> plans, IReadOnlyList<PaymentEntry> payments)
    {
        Dictionary<Guid, InstallmentPlan> active = plans.Where(p => p.Status == InstallmentPlanStatus.Active).ToDictionary(p => p.Id);
        var items = active.Values
            .Select(p => (Date: DateOnly.FromDateTime(this.calendar.TurkeyTime(p.CreatedAt).DateTime), Order: 0, Kind: "Plan", Name: p.FeeName, Receipt: (long?)null, Debit: p.NetAmount, Credit: 0m))
            .Concat(payments.Where(e => !e.Payment.IsCancelled && active.ContainsKey(e.Payment.PlanId))
                .Select(e => (Date: e.Payment.Date, Order: 1, Kind: "Payment", Name: active[e.Payment.PlanId].FeeName, Receipt: (long?)e.Payment.ReceiptNo, Debit: 0m, Credit: e.Payment.Amount)))
            .OrderBy(x => x.Date).ThenBy(x => x.Order).ThenBy(x => x.Receipt);
        decimal balance = 0m;
        List<LedgerMovement> movements = [];
        foreach (var x in items)
        {
            balance += x.Debit - x.Credit;
            movements.Add(new LedgerMovement(x.Date, x.Kind, x.Name, x.Receipt, x.Debit, x.Credit, balance));
        }

        return movements;
    }
}
