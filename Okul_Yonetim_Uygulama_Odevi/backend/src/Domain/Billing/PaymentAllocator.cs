namespace OkulYonetim.Domain.Billing;

/// <summary>
/// Tahsilat dağıtımı (spec 0015 C-4, gereksinim 5–6; saf hesap): tutar, vadesi en eski ödenmemiş taksitten başlayarak (vade, sonra
/// sıra) kalanlara dağıtılır — kısmi ödeme olur. Toplam kalandan fazla tutar reddedilir (<see cref="Remaining"/> ile önceden
/// denetlenir; avans/alacak yok). İptalde dağılım satırları aynen geri alınır (<see cref="Revert"/>).
/// </summary>
public static class PaymentAllocator
{
    public static decimal Remaining(IEnumerable<Installment> installments) => installments?.Sum(i => i.Remaining) ?? 0m;

    /// <summary>Dağıtımı hesaplar (taksitleri değiştirmez); tutar geçersizse ya da kalanı aşarsa reddeder.</summary>
    public static IReadOnlyList<AllocationLine> Allocate(IEnumerable<Installment> installments, decimal amount)
    {
        ArgumentNullException.ThrowIfNull(installments);
        List<Installment> open = [.. installments.Where(i => i.Remaining > 0m).OrderBy(i => i.DueDate).ThenBy(i => i.Sequence)];
        if (!MoneyRules.IsPositiveMoney(amount) || amount > open.Sum(i => i.Remaining))
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Tutar kalan borçtan fazla olamaz.");
        }

        List<AllocationLine> lines = [];
        decimal left = amount;
        foreach (Installment installment in open)
        {
            if (left == 0m)
            {
                break;
            }

            decimal part = Math.Min(left, installment.Remaining);
            lines.Add(new AllocationLine(installment.Id, part));
            left -= part;
        }

        return lines;
    }

    /// <summary>Hesaplanan dağıtımı taksitlere uygular.</summary>
    public static void Apply(IEnumerable<Installment> installments, IEnumerable<AllocationLine> lines)
    {
        ArgumentNullException.ThrowIfNull(installments);
        ArgumentNullException.ThrowIfNull(lines);
        Dictionary<Guid, Installment> byId = installments.ToDictionary(i => i.Id);
        foreach (AllocationLine line in lines)
        {
            byId[line.InstallmentId].Apply(line.Amount);
        }
    }

    /// <summary>İptal edilen tahsilatın dağılımını geri alır.</summary>
    public static void Revert(IEnumerable<Installment> installments, IEnumerable<PaymentAllocation> allocations)
    {
        ArgumentNullException.ThrowIfNull(installments);
        ArgumentNullException.ThrowIfNull(allocations);
        Dictionary<Guid, Installment> byId = installments.ToDictionary(i => i.Id);
        foreach (PaymentAllocation allocation in allocations)
        {
            byId[allocation.InstallmentId].Revert(allocation.Amount);
        }
    }
}
