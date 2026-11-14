namespace TahaKonsept.Agent.Abstractions.Processes;

/// <summary>Süreç sonucu.</summary>
/// <param name="ExitCode">Çıkış kodu.</param>
/// <param name="StandardOutput">Tüm stdout.</param>
/// <param name="StandardError">Tüm stderr.</param>
/// <param name="Duration">Süre.</param>
/// <param name="TimedOut">Zaman aşımına uğradı mı.</param>
public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError, TimeSpan Duration, bool TimedOut)
{
    /// <summary>Çıkış kodu 0 ve zaman aşımı yoksa <c>true</c>.</summary>
    public bool Succeeded => ExitCode == 0 && !TimedOut;
}
