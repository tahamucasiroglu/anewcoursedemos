namespace Konsept.Agent.Infrastructure.Sandbox;

/// <summary>Kısıtlı sürecin sonucu.</summary>
/// <param name="ExitCode">Çıkış kodu.</param>
/// <param name="Output">Birleşik çıktının son kısmı (stdout + stderr).</param>
/// <param name="LimitExceeded">Süre, bellek ya da süreç sınırı aşıldı mı.</param>
/// <param name="RemainingProcesses">Bittiğinde Job'da kalan süreç sayısı (her zaman 0 olmalı).</param>
public sealed record SandboxRun(int ExitCode, string Output, bool LimitExceeded, int RemainingProcesses);
