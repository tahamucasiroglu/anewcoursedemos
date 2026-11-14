using TahaKonsept.Modules.Concepts.Application.Models;

namespace TahaKonsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// Gömülü şablonlardan prompt çiftleri üretir: konu önerisi, konsept, düzeltme ve kelime denetimi. Sistem promptları bir iş boyunca
/// sabittir (önbellek); değişken içerik kullanıcı promptundadır.
/// </summary>
public interface IConceptPromptFactory
{
    /// <summary>Konu önerisi promptu.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="memoryBlock">Hafıza listesi.</param>
    /// <param name="hint">Yön.</param>
    /// <param name="idea">Fikir.</param>
    /// <param name="count">Adet.</param>
    /// <returns>Prompt çifti.</returns>
    PromptPair Topics(ConceptGenerationContext context, string memoryBlock, string? hint, string? idea, int count);

    /// <summary>Konsept üretim promptu.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="brief">Girdi.</param>
    /// <returns>Prompt çifti.</returns>
    PromptPair Concept(ConceptGenerationContext context, ConceptBrief brief);

    /// <summary>Düzeltme promptu.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="documentJson">Mevcut belge (kısa JSON).</param>
    /// <param name="instruction">Talimat.</param>
    /// <returns>Prompt çifti.</returns>
    PromptPair Revise(ConceptGenerationContext context, string documentJson, string instruction);

    /// <summary>Kelime denetimi promptu.</summary>
    /// <param name="words">Fazla tekrar edenler.</param>
    /// <param name="scenesBlock">"[index] metin" satırları.</param>
    /// <returns>Prompt çifti.</returns>
    PromptPair WordCheck(IReadOnlyList<string> words, string scenesBlock);
}
