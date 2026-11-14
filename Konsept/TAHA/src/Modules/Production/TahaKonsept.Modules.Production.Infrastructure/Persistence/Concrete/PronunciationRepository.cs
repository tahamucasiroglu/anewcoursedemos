using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Domain.Entities;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Infrastructure.Persistence.Concrete;

/// <summary>
/// <see cref="IPronunciationRepository"/>'nin EF Core uygulaması: kullanıcının sözlüğü; yazılış anahtarına göre arama.
/// </summary>
/// <param name="context">Modül DbContext'i.</param>
public sealed class PronunciationRepository(ProductionDbContext context) : Repository<Pronunciation>(context), IPronunciationRepository
{
    /// <summary>Kullanıcının tüm kayıtları (takipsiz).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıtlar.</returns>
    public async Task<IReadOnlyList<Pronunciation>> ListForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await QueryNoTracking().Where(x => x.UserId == userId).OrderBy(x => x.WrittenKey).ToListAsync(cancellationToken);

    /// <summary>Onaylı kayıtlar (takipsiz, yazılışa göre).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıtlar.</returns>
    public async Task<IReadOnlyList<Pronunciation>> ListApprovedAsync(Guid userId, CancellationToken cancellationToken) =>
        await QueryNoTracking().Where(x => x.UserId == userId && x.Status == PronunciationStatus.Approved).OrderBy(x => x.WrittenKey).ToListAsync(cancellationToken);

    /// <summary>Yazılış anahtarına göre (takipli).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="writtenKey">Anahtar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıt ya da <c>null</c>.</returns>
    public Task<Pronunciation?> FindByKeyAsync(Guid userId, string writtenKey, CancellationToken cancellationToken) =>
        Query().FirstOrDefaultAsync(x => x.UserId == userId && x.WrittenKey == writtenKey, cancellationToken);

    /// <summary>Kullanıcının kaydı (takipli).</summary>
    /// <param name="id">Kayıt.</param>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıt ya da <c>null</c>.</returns>
    public Task<Pronunciation?> GetForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken) =>
        Query().FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
}
