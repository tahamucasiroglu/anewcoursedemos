using TahaKonsept.Agent.Engines.Video.Media.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Video.Tests.Support;

/// <summary>Sabit süre dönen sahte medya okuyucu (hizalayıcı testleri).</summary>
/// <param name="seconds">Dönülecek süre.</param>
public sealed class FixedDurationProbe(double seconds) : IMediaProbe
{
    /// <inheritdoc />
    public Task<Result<double>> GetDurationAsync(string path, CancellationToken ct) => Task.FromResult(Result.Success(seconds));

    /// <inheritdoc />
    public Task<Result<bool>> HasAudioAsync(string path, CancellationToken ct) => Task.FromResult(Result.Success(true));
}
