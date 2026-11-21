namespace OkulYonetim.Domain.Billing;

/// <summary>Tahsilatın bir taksite dağılan parçası (spec 0015 gereksinim 5–6): tahsilat + taksit tekil, tutar sıfırdan büyük. İptalde silinmez; tahsilat iptal olunca taksidin ödeneni bu tutar kadar geri alınır.</summary>
public sealed class PaymentAllocation
{
    private PaymentAllocation(Guid id, Guid tenantId, Guid paymentId, Guid installmentId, decimal amount)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.PaymentId = paymentId;
        this.InstallmentId = installmentId;
        this.Amount = amount;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid PaymentId { get; private set; }

    public Guid InstallmentId { get; private set; }

    public decimal Amount { get; private set; }

    public static PaymentAllocation Create(Payment payment, Guid installmentId, decimal amount, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(payment);
        return MoneyRules.IsPositiveMoney(amount)
            ? new(Guid.CreateVersion7(now), payment.TenantId, payment.Id, installmentId, amount)
            : throw new ArgumentOutOfRangeException(nameof(amount), "Dağılım tutarı sıfırdan büyük olmalı.");
    }
}
