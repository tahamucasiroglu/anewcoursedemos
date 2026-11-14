using TahaKonsept.Modules.Concepts.Application.Models;

namespace TahaKonsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// Konu başlığı normalleştirici: Türkçe küçük harf + aksan katlama, kesme işaretli ekleri ve dolgu kelimelerini atma,
/// kök kısaltma → <see cref="TopicFingerprint"/>; benzerlik (Jaccard) ve tekrar kararı.
/// </summary>
public interface ITopicNormalizer
{
    /// <summary>Başlıktan parmak izi üretir.</summary>
    /// <param name="title">Başlık.</param>
    /// <returns>Parmak izi.</returns>
    TopicFingerprint Fingerprint(string title);

    /// <summary>Saklanmış anahtar kelimelerden parmak izi kurar.</summary>
    /// <param name="keywords">Anahtar kelimeler.</param>
    /// <param name="hash">Karma.</param>
    /// <returns>Parmak izi.</returns>
    TopicFingerprint FromStored(string keywords, string hash);

    /// <summary>İki parmak izinin benzerliği (0–1).</summary>
    /// <param name="a">Birinci.</param>
    /// <param name="b">İkinci.</param>
    /// <returns>Benzerlik.</returns>
    double Similarity(TopicFingerprint a, TopicFingerprint b);

    /// <summary>Aynı konu sayılır mı (karma eşit ya da benzerlik eşiğin üstünde).</summary>
    /// <param name="a">Birinci.</param>
    /// <param name="b">İkinci.</param>
    /// <returns>Tekrar mı.</returns>
    bool IsDuplicate(TopicFingerprint a, TopicFingerprint b);
}
