namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Konsept için sıradaki video sürüm numarasını verir: biten videolar ile henüz bitmemiş (bekleyen/çalışan) sıra öğelerinin en büyüğü + 1.
/// Böylece biri bitmeden art arda istenen "Yeni sürüm"ler ve aynı konseptin yeniden sıraya eklenmesi aynı numarayı almaz.
/// </summary>
public interface IVideoVersionAllocator
{
    /// <summary>Sıradaki sürüm.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sürüm (en az 1).</returns>
    Task<int> NextAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken);
}
