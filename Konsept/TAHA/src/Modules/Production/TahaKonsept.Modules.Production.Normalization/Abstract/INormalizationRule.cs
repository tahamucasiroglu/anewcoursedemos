using TahaKonsept.Modules.Production.Normalization.Models;

namespace TahaKonsept.Modules.Production.Normalization.Abstract;

/// <summary>
/// Normalizasyon zincirinin tek adımı (sözlük, tarih, Romen rakamı, sıra sayısı, sayı, kısaltma, sembol).
/// Adım <see cref="NormalizationContext.Text"/> üzerinde çalışır ve her değişikliği bağlama kaydeder.
/// Önemli üyeler: <see cref="Key"/>, <see cref="Apply"/>.
/// </summary>
public interface INormalizationRule
{
    /// <summary>Kural anahtarı (<see cref="NormalizationChange.Rule"/> alanına yazılır; ör. <c>number</c>).</summary>
    string Key { get; }

    /// <summary>Kuralı bağlamdaki metne uygular.</summary>
    /// <param name="context">Çalışma bağlamı (metin + değişiklik listesi).</param>
    void Apply(NormalizationContext context);
}
