using TahaKonsept.Agent.Abstractions.Compose;
using TahaKonsept.Agent.Abstractions.Compose.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Video.Compose.Base;

/// <summary>
/// <see cref="IComposer"/> ortak davranışı: isteği doğrular (sahne listesi, süre listesi, boyut, fps, dosyaların varlığı),
/// çalışma klasörünü hazırlar ve <see cref="ComposeCoreAsync"/>'i çağırır. İlerleme alt aralıklara <see cref="Scale"/> ile bölünür.
/// </summary>
public abstract class ComposerBase : IComposer
{
    /// <inheritdoc />
    public async Task<Result<ComposeOutput>> ComposeAsync(ComposeRequest request, IProgress<double>? progress, CancellationToken ct)
    {
        var valid = Validate(request);
        if (valid.IsFailure)
        {
            return Result.Failure<ComposeOutput>(valid.Error);
        }

        Directory.CreateDirectory(request.WorkDir);
        var outputDir = Path.GetDirectoryName(Path.GetFullPath(request.OutputPath));
        if (!string.IsNullOrEmpty(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        return await ComposeCoreAsync(request, progress, ct).ConfigureAwait(false);
    }

    /// <summary>İsteği doğrular.</summary>
    /// <param name="request">İstek.</param>
    /// <returns>Başarı ya da doğrulama hatası.</returns>
    public static Result Validate(ComposeRequest request)
    {
        if (request.ScenePaths.Count == 0)
        {
            return Error.Validation("compose.no_scenes", "Birleştirilecek sahne yok.");
        }

        if (request.SceneDurationsSec.Count != request.ScenePaths.Count)
        {
            return Error.Validation("compose.duration_mismatch", "Sahne süreleri listesi sahne sayısıyla aynı uzunlukta olmalı.");
        }

        if (request.Width < 16 || request.Height < 16 || request.Width % 2 != 0 || request.Height % 2 != 0)
        {
            return Error.Validation("compose.invalid_size", "Genişlik ve yükseklik çift sayı ve en az 16 olmalı.");
        }

        if (request.Fps is < 1 or > 120)
        {
            return Error.Validation("compose.invalid_fps", "Kare hızı 1–120 arasında olmalı.");
        }

        var missing = request.ScenePaths.FirstOrDefault(p => !File.Exists(p));
        if (missing is not null)
        {
            return Error.NotFound("compose.scene_missing", $"Sahne videosu bulunamadı: {missing}");
        }

        if (!string.IsNullOrWhiteSpace(request.AudioPath) && !File.Exists(request.AudioPath))
        {
            return Error.NotFound("compose.audio_missing", $"Ses dosyası bulunamadı: {request.AudioPath}");
        }

        return string.IsNullOrWhiteSpace(request.OutputPath) || string.IsNullOrWhiteSpace(request.WorkDir)
            ? Error.Validation("compose.invalid_paths", "Çalışma klasörü ve çıktı yolu zorunlu.")
            : Result.Success();
    }

    /// <summary>Asıl birleştirme işlemi (istek doğrulanmış, klasörler hazır).</summary>
    /// <param name="request">İstek.</param>
    /// <param name="progress">0–100 ilerleme.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    protected abstract Task<Result<ComposeOutput>> ComposeCoreAsync(ComposeRequest request, IProgress<double>? progress, CancellationToken ct);

    /// <summary>0–1 alt ilerlemeyi [from, to] aralığına ölçekleyip bildirir.</summary>
    /// <param name="progress">Hedef ilerleme.</param>
    /// <param name="from">Alt sınır (%).</param>
    /// <param name="to">Üst sınır (%).</param>
    /// <param name="ratio">0–1 oran.</param>
    protected static void Scale(IProgress<double>? progress, double from, double to, double ratio) =>
        progress?.Report(from + ((to - from) * Math.Clamp(ratio, 0, 1)));
}
