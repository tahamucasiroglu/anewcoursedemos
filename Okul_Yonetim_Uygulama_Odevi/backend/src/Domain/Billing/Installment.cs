namespace OkulYonetim.Domain.Billing;

/// <summary>
/// Planın bir taksidi (spec 0015 gereksinim 3, 5, 8): sıra, vade günü (<c>DateOnly</c>), tutar ve ödenen. Ödenen 0 ile tutar
/// arasındadır (veritabanı <c>CHECK</c> ikinci katman). Durum saklanmaz: <see cref="StateOn"/> — vade gününün bittiği andan
/// (Türkiye saati, ertesi gün 00:00) itibaren kalan tutarı kadar gecikmiştir; gecikme tanımı yalnızca buradadır.
/// </summary>
public sealed class Installment
{
    private Installment(Guid id, Guid tenantId, Guid planId, int sequence, DateOnly dueDate, decimal amount)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.PlanId = planId;
        this.Sequence = sequence;
        this.DueDate = dueDate;
        this.Amount = amount;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid PlanId { get; private set; }

    public int Sequence { get; private set; }

    public DateOnly DueDate { get; private set; }

    public decimal Amount { get; private set; }

    public decimal PaidAmount { get; private set; }

    public decimal Remaining => this.Amount - this.PaidAmount;

    public static Installment Create(Guid tenantId, Guid planId, InstallmentLine line, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(line);
        return MoneyRules.IsPositiveMoney(line.Amount)
            ? new(Guid.CreateVersion7(now), tenantId, planId, line.Sequence, line.DueDate, line.Amount)
            : throw new ArgumentOutOfRangeException(nameof(line), "Taksit tutarı sıfırdan büyük olmalı.");
    }

    /// <summary>Türkiye gününe göre durum: kalan yoksa Ödendi; vade günü geçtiyse Gecikti; kısmen ödendiyse Kısmen ödendi; değilse Bekliyor.</summary>
    public InstallmentState StateOn(DateOnly today) =>
        this.Remaining <= 0m ? InstallmentState.Paid
        : today > this.DueDate ? InstallmentState.Overdue
        : this.PaidAmount > 0m ? InstallmentState.PartiallyPaid
        : InstallmentState.Pending;

    /// <summary>Bugün gecikmiş tutar (gecikmediyse 0).</summary>
    public decimal OverdueOn(DateOnly today) => this.StateOn(today) == InstallmentState.Overdue ? this.Remaining : 0m;

    public void Apply(decimal amount)
    {
        if (!MoneyRules.IsPositiveMoney(amount) || amount > this.Remaining)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Dağıtılan tutar taksidin kalanını aşamaz.");
        }

        this.PaidAmount += amount;
    }

    public void Revert(decimal amount)
    {
        if (!MoneyRules.IsPositiveMoney(amount) || amount > this.PaidAmount)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Geri alınan tutar ödeneni aşamaz.");
        }

        this.PaidAmount -= amount;
    }
}
