using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Audio.Abstract;

/// <summary>
/// Sahne ses dosyalarını tek seslendirme dosyasına birleştirir ve süre ölçer. Aynı biçimli WAV'ler doğrudan,
/// farklı biçimler FFmpeg ile birleştirilir.
/// </summary>
public interface IAudioConcatenator
{
    /// <summary>Dosyaları sırayla birleştirir.</summary>
    /// <param name="inputs">Girdi dosyaları.</param>
    /// <param name="outputPath">Çıktı .wav.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Toplam süre (sn) ya da hata.</returns>
    Task<Result<double>> ConcatenateAsync(IReadOnlyList<string> inputs, string outputPath, CancellationToken ct);

    /// <summary>Ses dosyasının süresini döner.</summary>
    /// <param name="path">Dosya.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Süre (sn) ya da hata.</returns>
    Task<Result<double>> GetDurationAsync(string path, CancellationToken ct);
}
