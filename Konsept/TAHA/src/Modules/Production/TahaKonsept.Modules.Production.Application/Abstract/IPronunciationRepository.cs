using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Production.Domain.Entities;

namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Okunuş sözlüğü deposu.
/// </summary>
public interface IPronunciationRepository : IRepository<Pronunciation>
{
    /// <summary>Kullanıcının tüm kayıtları (yazılışa göre, takipsiz).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<IReadOnlyList<Pronunciation>> ListForUserAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Onaylı kayıtlar (takipsiz).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<IReadOnlyList<Pronunciation>> ListApprovedAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Yazılış anahtarına göre (takipli).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="writtenKey">Anahtar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Pronunciation?> FindByKeyAsync(Guid userId, string writtenKey, CancellationToken cancellationToken);

    /// <summary>Kullanıcının kaydı (takipli).</summary>
    /// <param name="id">Kayıt.</param>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Pronunciation?> GetForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken);
}
