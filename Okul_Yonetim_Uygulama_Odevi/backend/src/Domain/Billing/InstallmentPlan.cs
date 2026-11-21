namespace OkulYonetim.Domain.Billing;

/// <summary>
/// Öğrencinin taksit planı (spec 0015 gereksinim 3–4): kurum, şube (öğrencinin şubesi — oturumdan değil öğrenciden), öğrenci,
/// tarife kimliği ve açıldığı andaki tarife adı/tutarı, isteğe bağlı indirimin adı/yüzdesi/tutarı, net, taksit sayısı ve ilk vade.
/// İndirim tutarı = tarife × yüzde / 100 (kuruşa yarım yukarı), net = tarife − indirim (sıfırsa plan açılmaz). Taksitler
/// <see cref="InstallmentSplitter"/> ile; toplam = net (BR-7). Plan tarife değişikliğinden etkilenmez. İptal (<see cref="Cancel"/>)
/// yalnızca geçerli tahsilatı yokken (uygulamada denetlenir); iptal plan cariye, gecikmeye ve hatırlatmaya girmez.
/// </summary>
public sealed class InstallmentPlan
{
    public const int MaxFirstDueDays = 365;

    private InstallmentPlan(Guid id, Guid tenantId, Guid branchId, Guid studentId, Guid feeScheduleId, string feeName, decimal feeAmount, DateTimeOffset createdAt, Guid createdBy)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.BranchId = branchId;
        this.StudentId = studentId;
        this.FeeScheduleId = feeScheduleId;
        this.FeeName = feeName;
        this.FeeAmount = feeAmount;
        this.CreatedAt = createdAt;
        this.CreatedBy = createdBy;
        this.Status = InstallmentPlanStatus.Active;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid BranchId { get; private set; }

    public Guid StudentId { get; private set; }

    public Guid FeeScheduleId { get; private set; }

    public string FeeName { get; private set; }

    public decimal FeeAmount { get; private set; }

    public Guid? DiscountId { get; private set; }

    public string? DiscountName { get; private set; }

    public int? DiscountPercent { get; private set; }

    public decimal DiscountAmount { get; private set; }

    public decimal NetAmount { get; private set; }

    public int InstallmentCount { get; private set; }

    public DateOnly FirstDueDate { get; private set; }

    public InstallmentPlanStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public Guid? CancelledBy { get; private set; }

    /// <summary>İlk vade bugün ile bugünden <see cref="MaxFirstDueDays"/> gün sonrası arasında mı.</summary>
    public static bool IsFirstDueAllowed(DateOnly firstDue, DateOnly today) => firstDue >= today && firstDue <= today.AddDays(MaxFirstDueDays);

    /// <summary>Tarife ve indirimden net tutar (indirim yoksa tarifenin tamamı).</summary>
    public static (decimal Discount, decimal Net) NetOf(decimal feeAmount, int? discountPercent)
    {
        decimal discount = discountPercent is { } percent ? MoneyRules.DiscountOf(feeAmount, percent) : 0m;
        return (discount, feeAmount - discount);
    }

    /// <summary>Planı ve taksitlerini oluşturur; net sıfırsa ya da sayı/ilk vade geçersizse reddeder.</summary>
    public static (InstallmentPlan Plan, IReadOnlyList<Installment> Installments) Open(
        Guid tenantId,
        Guid branchId,
        Guid studentId,
        FeeSchedule fee,
        DiscountDefinition? discount,
        int count,
        DateOnly firstDue,
        DateOnly today,
        DateTimeOffset now,
        Guid actorId)
    {
        ArgumentNullException.ThrowIfNull(fee);
        if (!IsFirstDueAllowed(firstDue, today))
        {
            throw new ArgumentOutOfRangeException(nameof(firstDue), "İlk vade bugün ile 365 gün sonrası arasında olmalı.");
        }

        (decimal discountAmount, decimal net) = NetOf(fee.Amount, discount?.Percent);
        IReadOnlyList<InstallmentLine> lines = InstallmentSplitter.Split(net, count, firstDue);
        InstallmentPlan plan = new(Guid.CreateVersion7(now), tenantId, branchId, studentId, fee.Id, fee.Name, fee.Amount, now, actorId)
        {
            DiscountId = discount?.Id,
            DiscountName = discount?.Name,
            DiscountPercent = discount?.Percent,
            DiscountAmount = discountAmount,
            NetAmount = net,
            InstallmentCount = count,
            FirstDueDate = firstDue,
        };
        return (plan, [.. lines.Select(l => Installment.Create(tenantId, plan.Id, l, now))]);
    }

    public void Cancel(DateTimeOffset now, Guid actorId)
    {
        if (this.Status == InstallmentPlanStatus.Cancelled)
        {
            throw new InvalidOperationException("Plan zaten iptal.");
        }

        this.Status = InstallmentPlanStatus.Cancelled;
        this.CancelledAt = now;
        this.CancelledBy = actorId;
    }
}
