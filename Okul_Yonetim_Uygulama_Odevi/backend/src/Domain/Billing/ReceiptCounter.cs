namespace OkulYonetim.Domain.Billing;

/// <summary>
/// Okulun son makbuz numarası (spec 0015 C-5, gereksinim 7; plan Risks 4): okul başına tek satır, 1'den başlar. Sayaç tahsilatla
/// aynı işlemde ve okul başına sırada artar; işlem geri dönerse numara da döner — boşluk ve tekrar yok (sekans kullanılmaz).
/// </summary>
public sealed class ReceiptCounter
{
    private ReceiptCounter(Guid tenantId)
    {
        this.TenantId = tenantId;
    }

    public Guid TenantId { get; private set; }

    public long LastNumber { get; private set; }

    public static ReceiptCounter Start(Guid tenantId) => new(tenantId);

    /// <summary>Sonraki numarayı ayırır ve döndürür.</summary>
    public long Next()
    {
        this.LastNumber++;
        return this.LastNumber;
    }
}
