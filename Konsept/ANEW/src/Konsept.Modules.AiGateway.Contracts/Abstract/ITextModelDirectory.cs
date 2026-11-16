using Konsept.Modules.AiGateway.Contracts.Dtos;

namespace Konsept.Modules.AiGateway.Contracts.Abstract;

/// <summary>
/// Katalogdaki <b>metin</b> modelleri, isteği yapan kullanıcıya göre (spec 0005 Req 5, BR-13): bir model, kullanıcının o model için
/// yetkisi ve o sağlayıcı için geçerli (okunabilir) API key'i varsa seçilebilirdir. Görsel, yerel ve Ollama girişleri metin modeli
/// değildir; listede hiç yer almaz (E-4). Liste katalogdan gelir; kodda sabit model yoktur.
/// </summary>
public interface ITextModelDirectory
{
    /// <summary>İsteği yapan kullanıcı için tüm metin modelleri (seçilebilirlik bayrağıyla, katalog sırasıyla).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Modeller; giriş yoksa boş.</returns>
    Task<IReadOnlyList<TextModelInfo>> ListAsync(CancellationToken cancellationToken);
}
