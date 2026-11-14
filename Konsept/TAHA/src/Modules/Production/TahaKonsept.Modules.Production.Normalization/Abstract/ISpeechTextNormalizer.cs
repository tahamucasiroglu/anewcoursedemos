using TahaKonsept.Modules.Production.Normalization.Models;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Production.Normalization.Abstract;

/// <summary>
/// Seslendirme metnini TTS'e gitmeden önce normalize eden GENEL arayüz: yazılış (ekran/altyazı) aynen kalır, okunuş metni
/// üretilir. Zincir: okunuş sözlüğü (kullanıcı + konseptin LLM önerileri) → sayılar (Türkçe ekleriyle) → tarihler (MÖ/MS) →
/// Romen rakamları / sıra sayıları → kısaltmalar → sembol/birimler. Dil Türkçe değilse yalnız sözlük uygulanır.
/// Önemli üye: <see cref="Normalize"/>.
/// </summary>
public interface ISpeechTextNormalizer
{
    /// <summary>Metni normalize eder.</summary>
    /// <param name="text">Yazılış metni.</param>
    /// <param name="language">Dil kodu (ör. <c>tr</c>, <c>en</c>).</param>
    /// <param name="lexicon">Okunuş sözlüğü girdileri (öncelik sırasıyla; önce gelen kazanır).</param>
    /// <returns>Yazılış, okunuş ve yapılan değişikliklerin listesi.</returns>
    NormalizedText Normalize(string text, string language, IReadOnlyList<PronunciationEntry> lexicon);
}
