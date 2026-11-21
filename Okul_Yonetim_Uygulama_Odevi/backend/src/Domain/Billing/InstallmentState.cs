namespace OkulYonetim.Domain.Billing;

/// <summary>
/// Taksitin durumu (spec 0015 gereksinim 5, 8): saklanmaz, <see cref="Installment.StateOn"/> ile Türkiye gününe göre hesaplanır.
/// Ödendi (kalan yok), Gecikti (kalan var ve vade günü geçti — kısmen ödenmiş olsa da), Kısmen ödendi, Bekliyor.
/// </summary>
public enum InstallmentState
{
    Paid = 1,
    PartiallyPaid = 2,
    Pending = 3,
    Overdue = 4,
}
