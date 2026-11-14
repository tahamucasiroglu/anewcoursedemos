using Konsept.Modules.Monitoring.Contracts.Dtos;

namespace Konsept.Modules.Monitoring.Contracts.Abstract;

/// <summary>Diğer modüllerin ve log sink'lerinin veritabanındaki teknik loga yazdığı tek yol (spec 0008 Req 6–7).</summary>
public interface ITechnicalLogWriter
{
    /// <summary>Kayıtları yazar. Information seviyesindeki kayıtlar yazılmaz (Req 6).</summary>
    /// <param name="entries">Kayıtlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Yazılan kayıt sayısı.</returns>
    Task<int> WriteAsync(IReadOnlyList<TechnicalLogEntry> entries, CancellationToken cancellationToken);
}
