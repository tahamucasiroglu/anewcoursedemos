using TahaKonsept.Agent.Core.Audio.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Audio.Base;

/// <summary>
/// Birleştirici tabanı: tüm girdiler aynı biçimli WAV ise <see cref="WavFile.Concatenate"/> ile doğrudan birleştirir;
/// değilse <see cref="ConcatenateWithToolAsync"/> (ör. FFmpeg) çağrılır. Süre WAV başlığından, olmazsa <see cref="ProbeDurationAsync"/> ile okunur.
/// </summary>
public abstract class AudioConcatenatorBase : IAudioConcatenator
{
    /// <inheritdoc />
    public async Task<Result<double>> ConcatenateAsync(IReadOnlyList<string> inputs, string outputPath, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        if (inputs.Count == 0)
        {
            return Error.Validation("agent.audio_empty", "Birleştirilecek ses dosyası yok.");
        }

        var infos = new List<WavInfo>(inputs.Count);
        foreach (var input in inputs)
        {
            if (!WavFile.TryReadInfo(input, out var info) || info is null)
            {
                return await ConcatenateWithToolAsync(inputs, outputPath, ct);
            }

            infos.Add(info);
        }

        if (infos.Any(i => !i.IsCompatibleWith(infos[0])))
        {
            return await ConcatenateWithToolAsync(inputs, outputPath, ct);
        }

        var temp = outputPath + ".partial.wav";
        var duration = WavFile.Concatenate(infos, inputs, temp);
        File.Move(temp, outputPath, overwrite: true);
        return duration;
    }

    /// <inheritdoc />
    public Task<Result<double>> GetDurationAsync(string path, CancellationToken ct) =>
        WavFile.TryReadInfo(path, out var info) && info is not null
            ? Task.FromResult(Result.Success(info.DurationSec))
            : ProbeDurationAsync(path, ct);

    /// <summary>Farklı biçimli dosyaları harici araçla birleştirir.</summary>
    /// <param name="inputs">Girdiler.</param>
    /// <param name="outputPath">Çıktı.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Süre ya da hata.</returns>
    protected abstract Task<Result<double>> ConcatenateWithToolAsync(IReadOnlyList<string> inputs, string outputPath, CancellationToken ct);

    /// <summary>WAV olmayan dosyanın süresini harici araçla ölçer.</summary>
    /// <param name="path">Dosya.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Süre ya da hata.</returns>
    protected abstract Task<Result<double>> ProbeDurationAsync(string path, CancellationToken ct);
}
