using TahaKonsept.Agent.Abstractions.Align;
using TahaKonsept.Agent.Abstractions.Align.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Video.Align.Base;

/// <summary>
/// <see cref="IAligner"/> ortak davranışı: istek doğrulama, yazılış metnini kelimelere bölme (<see cref="SplitWords"/>),
/// kelime ağırlığı (<see cref="WordWeight"/>: harf sayısı + noktalama duraklaması) ve bir zaman aralığını ağırlıklara göre
/// kelimelere dağıtma (<see cref="Distribute"/>). Çıktı zamanları artan sırada, her kelime en az 1 ms.
/// </summary>
public abstract class AlignerBase : IAligner
{
    /// <inheritdoc />
    public abstract string Key { get; }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CaptionWord>>> AlignAsync(AlignRequest request, CancellationToken ct)
    {
        if (request.Segments.Count == 0)
        {
            return Result.Success<IReadOnlyList<CaptionWord>>([]);
        }

        var bad = request.Segments.FirstOrDefault(s => s.StartSec is { } a && s.EndSec is { } b && b < a);
        if (bad is not null)
        {
            return Error.Validation("align.invalid_segment", $"Segment bitişi başlangıçtan önce: \"{bad.DisplayText}\"");
        }

        return await AlignCoreAsync(request, ct).ConfigureAwait(false);
    }

    /// <summary>Yazılış metnini boşluklara göre kelimelere böler.</summary>
    /// <param name="text">Metin.</param>
    /// <returns>Kelimeler.</returns>
    public static IReadOnlyList<string> SplitWords(string text) =>
        text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>Kelimenin konuşma süresi ağırlığı: harf/rakam sayısı (en az 1) + noktalama duraklaması (virgül 2, cümle sonu 4).</summary>
    /// <param name="word">Kelime.</param>
    /// <returns>Ağırlık.</returns>
    public static double WordWeight(string word)
    {
        var letters = word.Count(char.IsLetterOrDigit);
        var weight = Math.Max(1, letters);
        if (word.Length > 0)
        {
            weight += word[^1] switch
            {
                ',' or ';' or ':' => 2,
                '.' or '!' or '?' or '…' => 4,
                _ => 0,
            };
        }

        return weight;
    }

    /// <summary>Bir zaman aralığını kelimelere ağırlıklarıyla orantılı dağıtır.</summary>
    /// <param name="words">Kelimeler.</param>
    /// <param name="startMs">Aralık başlangıcı.</param>
    /// <param name="endMs">Aralık bitişi.</param>
    /// <returns>Zamanlanmış kelimeler.</returns>
    public static IReadOnlyList<CaptionWord> Distribute(IReadOnlyList<string> words, int startMs, int endMs)
    {
        if (words.Count == 0)
        {
            return [];
        }

        var span = Math.Max(words.Count, endMs - startMs);
        var weights = words.Select(WordWeight).ToArray();
        var totalWeight = weights.Sum();
        var result = new List<CaptionWord>(words.Count);
        var cursor = 0d;
        for (var i = 0; i < words.Count; i++)
        {
            var from = startMs + (int)Math.Round(span * cursor / totalWeight);
            cursor += weights[i];
            var to = i == words.Count - 1 ? startMs + span : startMs + (int)Math.Round(span * cursor / totalWeight);
            result.Add(new CaptionWord(words[i], from, Math.Max(from + 1, to)));
        }

        return result;
    }

    /// <summary>Asıl hizalama (istek doğrulanmış, en az bir segment var).</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Kelime zamanlamaları.</returns>
    protected abstract Task<Result<IReadOnlyList<CaptionWord>>> AlignCoreAsync(AlignRequest request, CancellationToken ct);
}
