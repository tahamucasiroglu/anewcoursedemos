namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Orkestratör adımı: sırası çalışan/duraklatılmış ya da etkin işi olan her kullanıcı için (kullanıcı kilidi altında) <see cref="IJobAdvancer"/>'ı çağırır.
/// Arka plan servisi kullanıcıları <see cref="ListUsersAsync"/> ile bulur ve her kullanıcıyı kendi kapsamında, diğerlerinden bağımsız
/// (<see cref="AdvanceAsync"/>) ilerletir; bir kullanıcının uzun sunucu zinciri diğerlerinin sırasını bekletmez.
/// </summary>
public interface IProductionOrchestrator
{
    /// <summary>İlerletilmesi gereken kullanıcılar (sırası Boşta olmayan ya da etkin öğesi olan).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kullanıcılar.</returns>
    Task<IReadOnlyList<Guid>> ListUsersAsync(CancellationToken cancellationToken);

    /// <summary>Tek kullanıcıyı kilidi altında bir adım ilerletir; hatayı günlüğe yazar, iptal isteğini (sıradan çıkarma) yutar.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci (uygulama durdurma).</param>
    /// <returns>Görev.</returns>
    Task AdvanceAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Bir tur çalıştırır (kullanıcılar sırayla; testler ve tek seferlik tetikleme için); işlenen kullanıcı sayısını döner.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<int> TickAsync(CancellationToken cancellationToken);
}
