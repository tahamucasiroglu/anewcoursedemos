using TahaKonsept.Agent.Abstractions.Processes;

namespace TahaKonsept.Agent.Abstractions.Processes.Abstract;

/// <summary>Harici süreç çalıştırıcı (node, ffmpeg, python). Çıktıyı satır satır bildirir, iptalde süreç ağacını öldürür.</summary>
public interface IProcessRunner
{
    /// <summary>Süreci çalıştırır ve bitmesini bekler.</summary>
    /// <param name="spec">Süreç tanımı.</param>
    /// <param name="onOutput">Her stdout/stderr satırı için geri çağrı (isteğe bağlı).</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Çıkış kodu ve çıktılar.</returns>
    Task<ProcessResult> RunAsync(ProcessSpec spec, Action<string>? onOutput, CancellationToken ct);
}
