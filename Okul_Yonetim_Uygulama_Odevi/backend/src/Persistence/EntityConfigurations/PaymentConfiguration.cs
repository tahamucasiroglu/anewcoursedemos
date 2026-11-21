using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OkulYonetim.Domain.Billing;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;
using OkulYonetim.Domain.Students;

namespace OkulYonetim.Persistence.EntityConfigurations;

/// <summary>
/// Tahsilat tablosu (payments, spec 0015): plana, öğrenciye, şubeye ve kaydedene bağlı; tutar <c>numeric(18,2)</c> (<c>CHECK</c>
/// sıfırdan büyük), tarih <c>date</c>, yöntem metin. Okul + makbuz no tekil (aynı numara iki kez verilemez — sayaç sırasının ikinci katmanı).
/// </summary>
public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("payments", t => t.HasCheckConstraint("ck_payments_amount", "amount > 0 AND receipt_no >= 1"));
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Date).HasColumnType("date");
        builder.Property(p => p.Method).HasConversion<string>().HasMaxLength(16);
        builder.Property(p => p.Note).HasMaxLength(Payment.NoteMax);
        builder.Property(p => p.CancelReason).HasMaxLength(Payment.ReasonMax);
        builder.Ignore(p => p.IsCancelled);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(p => p.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Student>().WithMany().HasForeignKey(p => p.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InstallmentPlan>().WithMany().HasForeignKey(p => p.PlanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(p => p.RecordedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => new { p.TenantId, p.ReceiptNo }).IsUnique();
        builder.HasIndex(p => p.PlanId);
    }
}
