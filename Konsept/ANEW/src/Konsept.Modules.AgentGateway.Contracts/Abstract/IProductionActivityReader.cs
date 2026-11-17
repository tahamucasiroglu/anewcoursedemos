namespace Konsept.Modules.AgentGateway.Contracts.Abstract;

/// <summary>
/// Kullanıcının "Üretiliyor" durumunda bir işi var mı (spec 0023 Req 3: o sürede ajan güncellemesi başlamaz). Production modülü uygular;
/// AgentGateway yalnızca bu sözleşmeyi bilir (olay/sözleşme deseni, kural 1).
/// </summary>
public interface IProductionActivityReader
{
    /// <summary>Kullanıcının "Üretiliyor" işi var mı.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Varsa <c>true</c>.</returns>
    Task<bool> IsProducingAsync(Guid userId, CancellationToken cancellationToken);
}
