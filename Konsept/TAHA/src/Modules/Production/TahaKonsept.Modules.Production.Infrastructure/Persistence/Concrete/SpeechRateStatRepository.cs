using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Domain.Entities;

namespace TahaKonsept.Modules.Production.Infrastructure.Persistence.Concrete;

/// <summary>
/// <see cref="ISpeechRateStatRepository"/>'nin EF Core uygulaması: (kullanıcı, model, ses, dil) anahtarıyla tek kayıt.
/// </summary>
/// <param name="context">Modül DbContext'i.</param>
public sealed class SpeechRateStatRepository(ProductionDbContext context) : Repository<SpeechRateStat>(context), ISpeechRateStatRepository
{
    /// <summary>Kaydı bulur (takipli).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="speechModelKey">Ses modeli.</param>
    /// <param name="voiceKey">Ses.</param>
    /// <param name="language">Dil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıt ya da <c>null</c>.</returns>
    public Task<SpeechRateStat?> FindAsync(Guid userId, string speechModelKey, string voiceKey, string language, CancellationToken cancellationToken) =>
        Query().FirstOrDefaultAsync(
            x => x.UserId == userId && x.SpeechModelKey == speechModelKey && x.VoiceKey == voiceKey && x.Language == language,
            cancellationToken);
}
