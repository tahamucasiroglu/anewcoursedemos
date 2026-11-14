using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Production.Normalization.Models;

/// <summary>
/// Normalizasyon zincirinin çalışma bağlamı: kurallar sırayla <see cref="Text"/> üzerinde değişiklik yapar ve
/// her değişikliği <see cref="AddChange"/> ile kaydeder. Önemli alanlar: <see cref="Display"/> (değişmez yazılış),
/// <see cref="Language"/>, <see cref="Lexicon"/> (kullanıcı + konsept sözlüğü), <see cref="Changes"/>.
/// </summary>
public sealed class NormalizationContext
{
    private readonly List<NormalizationChange> _changes = [];

    /// <summary>Yeni bağlam oluşturur.</summary>
    /// <param name="display">Yazılış metni.</param>
    /// <param name="language">Dil kodu.</param>
    /// <param name="lexicon">Kullanıcı/konsept okunuş sözlüğü (öncelik sırasıyla).</param>
    public NormalizationContext(string display, string language, IReadOnlyList<PronunciationEntry> lexicon)
    {
        Display = display;
        Text = display;
        Language = language;
        Lexicon = lexicon;
    }

    /// <summary>Özgün yazılış metni (değişmez).</summary>
    public string Display { get; }

    /// <summary>Üzerinde çalışılan okunuş metni.</summary>
    public string Text { get; set; }

    /// <summary>Dil kodu.</summary>
    public string Language { get; }

    /// <summary>Kullanıcı + konsept okunuş sözlüğü (önce gelen kazanır).</summary>
    public IReadOnlyList<PronunciationEntry> Lexicon { get; }

    /// <summary>Şimdiye kadar yapılan değişiklikler (sırasıyla).</summary>
    public IReadOnlyList<NormalizationChange> Changes => _changes;

    /// <summary>Bir değişiklik kaydeder (boşluklar kırpılır; özgün ve yeni değer aynıysa kaydedilmez).</summary>
    /// <param name="original">Özgün parça.</param>
    /// <param name="replacement">Okunuş karşılığı.</param>
    /// <param name="rule">Kural anahtarı.</param>
    public void AddChange(string original, string replacement, string rule)
    {
        var o = original.Trim();
        var r = replacement.Trim();
        if (o.Length == 0 || string.Equals(o, r, StringComparison.Ordinal))
        {
            return;
        }

        _changes.Add(new NormalizationChange(o, r, rule));
    }
}
