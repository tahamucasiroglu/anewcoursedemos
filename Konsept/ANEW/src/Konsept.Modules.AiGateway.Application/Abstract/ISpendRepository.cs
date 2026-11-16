using Konsept.Modules.AiGateway.Domain.Entities;

namespace Konsept.Modules.AiGateway.Application.Abstract;

/// <summary>Harcama kayıtlarının kalıcılık yüzeyi (spec 0013 Req 11–16). Okuma her zaman kullanıcı kimliğiyle süzülür (BR-1).</summary>
public interface ISpendRepository
{
    /// <summary>Kaydı ekler.</summary>
    /// <param name="record">Kayıt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task AddAsync(SpendRecord record, CancellationToken cancellationToken);

    /// <summary>Kullanıcının [başlangıç, bitiş) aralığındaki kayıtlarının yuvarlanmamış toplamı.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="fromUtc">Başlangıç (dahil).</param>
    /// <param name="toUtc">Bitiş (hariç).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>USD.</returns>
    Task<decimal> SumAsync(Guid userId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken);
}
