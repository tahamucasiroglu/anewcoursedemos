namespace OkulYonetim.Domain.Billing;

/// <summary>
/// Gönderilmiş gecikme hatırlatması (spec 0015 gereksinim 9; plan Risks 3): taksit + sıra (1–<see cref="ReminderSchedule.MaxReminders"/>)
/// tekildir — aynı hatırlatma yeniden başlatmada ya da eşzamanlı çalışmada ikinci kez yazılamaz. Önce bu kayıt, sonra bildirim
/// kapısı; kapı hatasında hatırlatma gönderilmiş sayılır (0011 kapı sözleşmesi).
/// </summary>
public sealed class PaymentReminder
{
    private PaymentReminder(Guid id, Guid tenantId, Guid installmentId, int sequence, DateTimeOffset sentAt)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.InstallmentId = installmentId;
        this.Sequence = sequence;
        this.SentAt = sentAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid InstallmentId { get; private set; }

    public int Sequence { get; private set; }

    public DateTimeOffset SentAt { get; private set; }

    public static PaymentReminder Sent(Installment installment, int sequence, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(installment);
        return sequence is >= 1 and <= ReminderSchedule.MaxReminders
            ? new(Guid.CreateVersion7(now), installment.TenantId, installment.Id, sequence, now)
            : throw new ArgumentOutOfRangeException(nameof(sequence), "Hatırlatma sırası 1–3 olmalı.");
    }
}
