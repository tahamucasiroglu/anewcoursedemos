namespace TahaKonsept.Modules.Concepts.Application.Models;

/// <summary>
/// Konu parmak izi: normalleştirilmiş, kök kısaltılmış, sıralı anahtar kelimeler (<see cref="Keywords"/>),
/// kısa karma (<see cref="Hash"/>) ve benzerlik için kelime kümesi (<see cref="Words"/>).
/// </summary>
/// <param name="Keywords">Boşlukla ayrılmış anahtar kelimeler.</param>
/// <param name="Hash">Kısa karma (16 hex).</param>
/// <param name="Words">Kelime kümesi.</param>
public sealed record TopicFingerprint(string Keywords, string Hash, IReadOnlySet<string> Words);
