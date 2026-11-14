using System.Security.Cryptography;
using System.Text;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Common;
using TahaKonsept.Modules.Concepts.Application.Models;

namespace TahaKonsept.Modules.Concepts.Application.Base;

/// <summary>
/// <see cref="ITopicNormalizer"/> ortak davranışı: başlık → katlanmış kelimeler → dolgu kelimeleri atılır → <see cref="StemLength"/>
/// karakterde kök kısaltma (Türkçe ekler için kaba kök) → sıralı küme; karma SHA-256'nın ilk 16 hex'i.
/// Tekrar: karma eşit ya da Jaccard ≥ <see cref="DuplicateThreshold"/>.
/// </summary>
public abstract class TopicNormalizerBase : ITopicNormalizer
{
    private static readonly HashSet<string> DefaultStopWords = new(StringComparer.Ordinal)
    {
        "ve", "ile", "bir", "bu", "su", "o", "da", "de", "ki", "mi", "mu", "icin", "gibi", "en", "cok", "daha", "ne", "neden",
        "nasil", "nedir", "kim", "kimdi", "hangi", "olan", "oldu", "olarak", "ama", "veya", "ya", "yani", "her", "tum", "kadar",
        "sonra", "once", "hic", "iste", "gercek", "gercekte", "aslinda", "bilinmeyen", "inanilmaz", "sasirtici",
        "the", "of", "and", "a", "an", "in", "on", "to", "how", "why", "what", "who",
    };

    /// <summary>Kök uzunluğu (karakter).</summary>
    protected virtual int StemLength => 6;

    /// <summary>Tekrar eşiği (Jaccard).</summary>
    protected virtual double DuplicateThreshold => 0.6;

    /// <summary>Dolgu kelimeleri (katlanmış biçimde).</summary>
    protected virtual IReadOnlySet<string> StopWords => DefaultStopWords;

    /// <summary>Başlıktan parmak izi üretir.</summary>
    /// <param name="title">Başlık.</param>
    /// <returns>Parmak izi.</returns>
    public virtual TopicFingerprint Fingerprint(string title)
    {
        var words = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var word in TurkishText.Words(title))
        {
            var folded = TurkishText.Fold(word);
            if (folded.Length < 2 || StopWords.Contains(folded))
            {
                continue;
            }

            words.Add(folded.Length <= StemLength ? folded : folded[..StemLength]);
        }

        var keywords = string.Join(' ', words);
        return new TopicFingerprint(keywords, HashOf(keywords), words);
    }

    /// <summary>Saklanmış anahtar kelimelerden parmak izi kurar.</summary>
    /// <param name="keywords">Anahtar kelimeler.</param>
    /// <param name="hash">Karma.</param>
    /// <returns>Parmak izi.</returns>
    public TopicFingerprint FromStored(string keywords, string hash)
    {
        var words = new SortedSet<string>((keywords ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
        return new TopicFingerprint(string.Join(' ', words), string.IsNullOrEmpty(hash) ? HashOf(string.Join(' ', words)) : hash, words);
    }

    /// <summary>Jaccard benzerliği.</summary>
    /// <param name="a">Birinci.</param>
    /// <param name="b">İkinci.</param>
    /// <returns>0–1.</returns>
    public double Similarity(TopicFingerprint a, TopicFingerprint b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (a.Words.Count == 0 || b.Words.Count == 0)
        {
            return 0;
        }

        var intersection = a.Words.Count(b.Words.Contains);
        var union = a.Words.Count + b.Words.Count - intersection;
        return union == 0 ? 0 : (double)intersection / union;
    }

    /// <summary>Tekrar mı.</summary>
    /// <param name="a">Birinci.</param>
    /// <param name="b">İkinci.</param>
    /// <returns>Tekrar mı.</returns>
    public bool IsDuplicate(TopicFingerprint a, TopicFingerprint b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (a.Keywords.Length > 0 && string.Equals(a.Hash, b.Hash, StringComparison.Ordinal))
        {
            return true;
        }

        return Similarity(a, b) >= DuplicateThreshold;
    }

    /// <summary>Anahtar kelimelerin kısa karması.</summary>
    /// <param name="keywords">Anahtar kelimeler.</param>
    /// <returns>16 hex karakter.</returns>
    protected static string HashOf(string keywords) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(keywords ?? string.Empty)))[..16].ToLowerInvariant();
}
