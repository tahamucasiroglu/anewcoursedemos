namespace TahaKonsept.Agent.Abstractions.Processes;

/// <summary>Çalıştırılacak süreç.</summary>
/// <param name="FileName">Çalıştırılabilir dosya.</param>
/// <param name="Arguments">Argümanlar (tek tek; kaçış çalıştırıcıda yapılır).</param>
/// <param name="WorkingDirectory">Çalışma klasörü.</param>
/// <param name="Environment">Ek ortam değişkenleri.</param>
/// <param name="Timeout">Zaman aşımı (<c>null</c> = yok).</param>
/// <param name="StandardInput">Süreç girişine yazılacak metin (isteğe bağlı).</param>
public sealed record ProcessSpec(
    string FileName,
    IReadOnlyList<string> Arguments,
    string? WorkingDirectory = null,
    IReadOnlyDictionary<string, string>? Environment = null,
    TimeSpan? Timeout = null,
    string? StandardInput = null);
