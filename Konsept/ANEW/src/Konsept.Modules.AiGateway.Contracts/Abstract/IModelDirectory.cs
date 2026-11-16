using Konsept.Modules.AiGateway.Contracts.Dtos;

namespace Konsept.Modules.AiGateway.Contracts.Abstract;

/// <summary>
/// Katalogdaki tüm girişler (metin, görsel, ses, video motoru; online ve yerel), isteği yapan kullanıcıya göre kullanılabilirlik
/// etiketiyle (spec 0006 Req 2–4, 18). Üretim ayarları (Production) bu listeden seçenek gösterir ve seçimi doğrular (BR-13).
/// </summary>
public interface IModelDirectory
{
    /// <summary>Katalog sırasıyla tüm girişler.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Girişler; liste okunamadıysa <c>null</c> (spec 0006 AC-22).</returns>
    Task<IReadOnlyList<ModelInfo>?> ListAllAsync(CancellationToken cancellationToken);
}
