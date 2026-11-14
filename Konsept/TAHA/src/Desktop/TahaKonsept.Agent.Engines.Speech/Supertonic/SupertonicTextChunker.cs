using System.Text.RegularExpressions;

namespace TahaKonsept.Agent.Engines.Speech.Supertonic;

/// <summary>
/// Uzun metni modele uygun parçalara böler (resmi örnekteki <c>chunk_text</c>): önce paragraf (boş satır), sonra cümle sonu
/// (<c>. ! ?</c> + boşluk; tek büyük harf kısaltmaları ve yaygın İngilizce/Türkçe kısaltmalar hariç), parçalar <c>maxLength</c>'i aşmaz.
/// </summary>
internal static partial class SupertonicTextChunker
{
    /// <summary>Metni parçalara böler.</summary>
    /// <param name="text">Metin.</param>
    /// <param name="maxLength">Parça başına en fazla karakter (ko/ja için 120, diğerleri 300).</param>
    /// <returns>Parçalar (en az bir eleman).</returns>
    public static IReadOnlyList<string> Chunk(string text, int maxLength = 300)
    {
        var chunks = new List<string>();
        var paragraphs = Paragraph().Split(text.Trim()).Select(p => p.Trim()).Where(p => p.Length > 0);
        foreach (var paragraph in paragraphs)
        {
            var current = string.Empty;
            foreach (var sentence in SentenceEnd().Split(paragraph))
            {
                if (string.IsNullOrWhiteSpace(sentence))
                {
                    continue;
                }

                if (current.Length + sentence.Length + 1 <= maxLength)
                {
                    current = current.Length == 0 ? sentence : current + " " + sentence;
                }
                else
                {
                    if (current.Length > 0)
                    {
                        chunks.Add(current.Trim());
                    }

                    current = sentence;
                }
            }

            if (current.Length > 0)
            {
                chunks.Add(current.Trim());
            }
        }

        if (chunks.Count == 0)
        {
            chunks.Add(text.Trim());
        }

        return chunks;
    }

    [GeneratedRegex(@"\n\s*\n+")]
    private static partial Regex Paragraph();

    [GeneratedRegex(@"(?<!Mr\.|Mrs\.|Ms\.|Dr\.|Prof\.|Sr\.|Jr\.|Ph\.D\.|etc\.|e\.g\.|i\.e\.|vs\.|Inc\.|Ltd\.|Co\.|Corp\.|St\.|Ave\.|Blvd\.|vb\.|vs\.|Doç\.|Av\.)(?<!\b[A-ZÇĞİÖŞÜ]\.)(?<=[.!?])\s+")]
    private static partial Regex SentenceEnd();
}
