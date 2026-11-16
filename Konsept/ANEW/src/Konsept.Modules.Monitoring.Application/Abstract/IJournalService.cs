using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Monitoring.Application.Abstract;

/// <summary>
/// Günlük geçmişi (spec 0021 Req 5–8): isteği yapan kullanıcının bir gündeki kayıtları, seviye filtresi ve 50'şer sayfa.
/// </summary>
public interface IJournalService
{
    /// <summary>Bir günün bir sayfasını döndürür.</summary>
    /// <param name="request">Gün, filtre, atlanacak kayıt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Gün sayfası ya da oturum yok hatası.</returns>
    Task<Result<JournalDayDto>> GetDayAsync(JournalDayRequest? request, CancellationToken cancellationToken);
}
