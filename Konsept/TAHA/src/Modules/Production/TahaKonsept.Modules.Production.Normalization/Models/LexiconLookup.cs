using System.Text.RegularExpressions;

namespace TahaKonsept.Modules.Production.Normalization.Models;

/// <summary>Derlenmiş sözlük deseni + katlanmış yazılış → okunuş tablosu (LexiconRule iç kullanımı).</summary>
/// <param name="Pattern">Eşleşme deseni (katlanmış metin üzerinde çalışır).</param>
/// <param name="Spoken">Katlanmış yazılış → okunuş.</param>
internal sealed record LexiconLookup(Regex Pattern, IReadOnlyDictionary<string, string> Spoken);
