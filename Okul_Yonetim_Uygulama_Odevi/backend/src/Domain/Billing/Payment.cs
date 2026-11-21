namespace OkulYonetim.Domain.Billing;

/// <summary>
/// Elle kaydedilen tahsilat (spec 0015 gereksinim 5–7): kurum, şube ve öğrenci (planınkiler), plan, tutar (sıfırdan büyük, en fazla
/// iki ondalık), tarih (<c>DateOnly</c>; bugün ya da en fazla <see cref="MaxPastDays"/> gün önce), yöntem, isteğe bağlı not (en
/// fazla <see cref="NoteMax"/>), okul başına sıra numaralı makbuz no ve kaydeden. Silinmez: gerekçeyle (<see cref="ReasonMax"/>)
/// iptal edilir (<see cref="Cancel"/>); iptal edilen yeniden iptal edilemez, makbuz numarası yeniden verilmez.
/// </summary>
public sealed class Payment
{
    public const int NoteMax = 200;
    public const int ReasonMax = 200;
    public const int MaxPastDays = 365;

    private Payment(Guid id, InstallmentPlan plan, decimal amount, DateOnly date, PaymentMethod method, string? note, long receiptNo, DateTimeOffset now, Guid actorId)
    {
        this.Id = id;
        this.TenantId = plan.TenantId;
        this.BranchId = plan.BranchId;
        this.StudentId = plan.StudentId;
        this.PlanId = plan.Id;
        this.Amount = amount;
        this.Date = date;
        this.Method = method;
        this.Note = note;
        this.ReceiptNo = receiptNo;
        this.RecordedAt = now;
        this.RecordedBy = actorId;
    }

    private Payment()
    {
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid BranchId { get; private set; }

    public Guid StudentId { get; private set; }

    public Guid PlanId { get; private set; }

    public decimal Amount { get; private set; }

    public DateOnly Date { get; private set; }

    public PaymentMethod Method { get; private set; }

    public string? Note { get; private set; }

    public long ReceiptNo { get; private set; }

    public DateTimeOffset RecordedAt { get; private set; }

    public Guid RecordedBy { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public Guid? CancelledBy { get; private set; }

    public string? CancelReason { get; private set; }

    public bool IsCancelled => this.CancelledAt is not null;

    /// <summary>Tarih bugün ya da en fazla 365 gün önce mi (gelecek olamaz).</summary>
    public static bool IsDateAllowed(DateOnly date, DateOnly today) => date <= today && date >= today.AddDays(-MaxPastDays);

    public static Payment Record(InstallmentPlan plan, decimal amount, DateOnly date, PaymentMethod method, string? note, long receiptNo, DateOnly today, DateTimeOffset now, Guid actorId)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (plan.Status != InstallmentPlanStatus.Active)
        {
            throw new InvalidOperationException("İptal plana tahsilat girilemez.");
        }

        if (!MoneyRules.IsPositiveMoney(amount) || !IsDateAllowed(date, today) || !Enum.IsDefined(method) || receiptNo < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Tahsilat bilgisi geçersiz.");
        }

        string? clean = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        return clean is { Length: > NoteMax }
            ? throw new ArgumentOutOfRangeException(nameof(note), "Not sınırı aşamaz.")
            : new(Guid.CreateVersion7(now), plan, amount, date, method, clean, receiptNo, now, actorId);
    }

    public void Cancel(string reason, DateTimeOffset now, Guid actorId)
    {
        if (this.IsCancelled)
        {
            throw new InvalidOperationException("Tahsilat zaten iptal.");
        }

        string clean = reason?.Trim() ?? string.Empty;
        if (clean.Length is 0 or > ReasonMax)
        {
            throw new ArgumentOutOfRangeException(nameof(reason), "İptal gerekçesi boş olamaz ve sınırı aşamaz.");
        }

        this.CancelledAt = now;
        this.CancelledBy = actorId;
        this.CancelReason = clean;
    }
}
