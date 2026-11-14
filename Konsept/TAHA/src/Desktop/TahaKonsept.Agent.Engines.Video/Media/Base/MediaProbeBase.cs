using System.Globalization;
using TahaKonsept.Agent.Engines.Video.Media.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Video.Media.Base;

/// <summary>
/// <see cref="IMediaProbe"/> için ortak davranış: dosya varlık kontrolü ve sayısal çıktı ayrıştırma.
/// Türetilen sınıf <see cref="ReadDurationTextAsync"/> ve <see cref="ReadAudioStreamsTextAsync"/> ile ham değeri okur.
/// </summary>
public abstract class MediaProbeBase : IMediaProbe
{
    /// <inheritdoc />
    public async Task<Result<double>> GetDurationAsync(string path, CancellationToken ct)
    {
        if (!File.Exists(path))
        {
            return Error.NotFound("media.not_found", $"Medya dosyası bulunamadı: {path}");
        }

        var text = await ReadDurationTextAsync(path, ct).ConfigureAwait(false);
        if (text.IsFailure)
        {
            return Result.Failure<double>(text.Error);
        }

        var firstLine = text.Value.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        return double.TryParse(firstLine, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) && seconds > 0
            ? seconds
            : Error.Failure("media.duration_unreadable", $"Medya süresi okunamadı: {path}");
    }

    /// <inheritdoc />
    public async Task<Result<bool>> HasAudioAsync(string path, CancellationToken ct)
    {
        if (!File.Exists(path))
        {
            return Error.NotFound("media.not_found", $"Medya dosyası bulunamadı: {path}");
        }

        var text = await ReadAudioStreamsTextAsync(path, ct).ConfigureAwait(false);
        return text.IsFailure ? Result.Failure<bool>(text.Error) : !string.IsNullOrWhiteSpace(text.Value);
    }

    /// <summary>Süreyi saniye olarak (ilk satırda, nokta ondalıklı) içeren ham metni okur.</summary>
    /// <param name="path">Dosya.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Ham metin.</returns>
    protected abstract Task<Result<string>> ReadDurationTextAsync(string path, CancellationToken ct);

    /// <summary>Ses akışlarını listeleyen ham metni okur (ses yoksa boş).</summary>
    /// <param name="path">Dosya.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Ham metin.</returns>
    protected abstract Task<Result<string>> ReadAudioStreamsTextAsync(string path, CancellationToken ct);
}
