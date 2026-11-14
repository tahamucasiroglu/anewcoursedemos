namespace TahaKonsept.Modules.Concepts.Application.Models;

/// <summary>
/// Seslendirmede fazla tekrar eden kelime: en sık görünen yazılışı, toplam tekrar ve geçtiği sahneler.
/// </summary>
/// <param name="Word">Kelime (en sık yazılış).</param>
/// <param name="Count">Tekrar sayısı (kök bazında).</param>
/// <param name="SceneIndexes">Geçtiği sahneler.</param>
public sealed record WordRepetitionFinding(string Word, int Count, IReadOnlyList<int> SceneIndexes);
