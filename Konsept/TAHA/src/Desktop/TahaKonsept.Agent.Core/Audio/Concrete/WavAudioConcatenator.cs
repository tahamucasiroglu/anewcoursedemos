using TahaKonsept.Agent.Core.Audio.Base;
using TahaKonsept.Agent.Core.Media.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Audio.Concrete;

/// <summary>WAV birleştirici; farklı biçimlerde FFmpeg'e (<see cref="IMediaProbe"/>) düşer.</summary>
public sealed class WavAudioConcatenator : AudioConcatenatorBase
{
    private readonly IMediaProbe _media;

    /// <summary>Oluşturur.</summary>
    /// <param name="media">Medya yardımcısı.</param>
    public WavAudioConcatenator(IMediaProbe media)
    {
        _media = media;
    }

    /// <inheritdoc />
    protected override async Task<Result<double>> ConcatenateWithToolAsync(IReadOnlyList<string> inputs, string outputPath, CancellationToken ct)
    {
        var result = await _media.ConcatAudioAsync(inputs, outputPath, ct);
        return result.IsFailure ? result.Error : await GetDurationAsync(outputPath, ct);
    }

    /// <inheritdoc />
    protected override Task<Result<double>> ProbeDurationAsync(string path, CancellationToken ct) => _media.GetDurationAsync(path, ct);
}
