using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Billing;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>Gecikme hatırlatması tablosu (payment_reminders, spec 0015): taksit + sıra tekil (aynı hatırlatma iki kez yazılamaz), sıra 1–3.</summary>
public sealed class PaymentReminderConfiguration : IEntityTypeConfiguration<PaymentReminder>
{
    public void Configure(EntityTypeBuilder<PaymentReminder> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("payment_reminders", t => t.HasCheckConstraint("ck_payment_reminders_sequence", "sequence >= 1 AND sequence <= 3"));
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Installment>().WithMany().HasForeignKey(r => r.InstallmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => new { r.InstallmentId, r.Sequence }).IsUnique();
    }
}
