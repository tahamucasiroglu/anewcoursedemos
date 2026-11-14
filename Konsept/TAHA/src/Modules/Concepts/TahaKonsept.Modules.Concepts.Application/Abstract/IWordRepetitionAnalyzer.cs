using TahaKonsept.Modules.Concepts.Application.Models;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// Yerel (LLM'siz, ücretsiz) kelime tekrar analizi: seslendirmelerde kök bazında fazla tekrar eden kelimeleri bulur
/// (dolgu kelimeleri ve başlıktaki konu kelimeleri hariç). Bulgu yoksa LLM hiç çağrılmaz.
/// </summary>
public interface IWordRepetitionAnalyzer
{
    /// <summary>Belgeyi analiz eder.</summary>
    /// <param name="document">Belge.</param>
    /// <returns>Bulgular (çoktan aza).</returns>
    IReadOnlyList<WordRepetitionFinding> Analyze(ConceptDocument document);
}
