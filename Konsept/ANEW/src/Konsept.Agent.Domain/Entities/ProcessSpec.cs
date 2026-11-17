namespace Konsept.Agent.Domain.Entities;

/// <summary>Çalıştırılacak dış süreç (spec 0028 Req 10, 11): yürütülebilir dosya, argümanlar, çalışma klasörü, ek ortam değişkenleri ve süre sınırı.</summary>
/// <param name="Executable">Yürütülebilir dosya.</param>
/// <param name="Arguments">Argümanlar (tek tek; kabuk yorumlanmaz).</param>
/// <param name="WorkingDirectory">Çalışma klasörü.</param>
/// <param name="Environment">Eklenecek/ezilecek ortam değişkenleri.</param>
/// <param name="Timeout">En uzun süre; aşılırsa süreç ağacı öldürülür.</param>
public sealed record ProcessSpec(string Executable, IReadOnlyList<string> Arguments, string WorkingDirectory, IReadOnlyDictionary<string, string> Environment, TimeSpan Timeout);
