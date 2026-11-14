using System.Globalization;
using TahaKonsept.Agent.Abstractions.Processes;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;
using TahaKonsept.Agent.Abstractions.Tools;
using TahaKonsept.Agent.Abstractions.Tools.Abstract;
using TahaKonsept.Agent.Core.Media.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Media.Base;

/// <summary>
/// <see cref="IImageFitter"/> tabanı (ffprobe/ffmpeg, <see cref="IToolLocator"/> + <see cref="IProcessRunner"/>).
/// Boyut <c>ffprobe -show_entries stream=width,height</c> ile okunur. Uydurma filtresi <see cref="BuildFitFilter"/>:
/// <c>scale=W:H:force_original_aspect_ratio=increase:flags=lanczos,crop=W:H,setsar=1</c> — oran korunur, taşan kenar ortadan kırpılır,
/// görsel hiç esnetilmez. Çıktı geçici dosyaya yazılır, sonra asıl dosyanın yerine taşınır (yarım dosya kalmaz).
/// </summary>
public abstract class ImageFitterBase : IImageFitter
{
    private readonly IToolLocator _tools;
    private readonly IProcessRunner _processes;

    /// <summary>Oluşturur.</summary>
    /// <param name="tools">Araç bulucu.</param>
    /// <param name="processes">Süreç çalıştırıcı.</param>
    protected ImageFitterBase(IToolLocator tools, IProcessRunner processes)
    {
        _tools = tools ?? throw new ArgumentNullException(nameof(tools));
        _processes = processes ?? throw new ArgumentNullException(nameof(processes));
    }

    /// <inheritdoc />
    public async Task<Result<(int Width, int Height)>> GetSizeAsync(string path, CancellationToken ct)
    {
        var ffprobe = _tools.Require(ToolKind.Ffprobe);
        if (ffprobe.IsFailure)
        {
            return ffprobe.Error;
        }

        var result = await _processes.RunAsync(
            new ProcessSpec(ffprobe.Value, ["-v", "error", "-select_streams", "v:0", "-show_entries", "stream=width,height", "-of", "csv=p=0:s=x", path], Timeout: TimeSpan.FromMinutes(1)),
            null,
            ct).ConfigureAwait(false);
        if (result.Succeeded && TryParseSize(result.StandardOutput, out var width, out var height))
        {
            return (width, height);
        }

        return Error.Failure("agent.image_probe_failed", "Görsel boyutu okunamadı: " + Path.GetFileName(path));
    }

    /// <inheritdoc />
    public async Task<Result<bool>> FitAsync(string path, int width, int height, CancellationToken ct)
    {
        var size = await GetSizeAsync(path, ct).ConfigureAwait(false);
        if (size.IsFailure)
        {
            return size.Error;
        }

        if (size.Value.Width == width && size.Value.Height == height)
        {
            return false;
        }

        var ffmpeg = _tools.Require(ToolKind.Ffmpeg);
        if (ffmpeg.IsFailure)
        {
            return ffmpeg.Error;
        }

        var temp = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".", Path.GetFileNameWithoutExtension(path) + ".fit" + Path.GetExtension(path));
        var result = await _processes.RunAsync(
            new ProcessSpec(ffmpeg.Value, ["-y", "-hide_banner", "-loglevel", "error", "-i", path, "-vf", BuildFitFilter(width, height), "-frames:v", "1", temp], Timeout: TimeSpan.FromMinutes(2)),
            null,
            ct).ConfigureAwait(false);
        if (!result.Succeeded || !File.Exists(temp))
        {
            TryDelete(temp);
            return Error.Failure(
                "agent.image_fit_failed",
                string.Create(CultureInfo.InvariantCulture, $"Görsel {size.Value.Width}x{size.Value.Height} boyutundan {width}x{height} boyutuna getirilemedi: {result.StandardError.Trim()}"));
        }

        File.Move(temp, path, overwrite: true);
        return true;
    }

    /// <summary>Kapla + ortadan kırp filtresi (oran korunur, esnetme yok).</summary>
    /// <param name="width">Hedef genişlik.</param>
    /// <param name="height">Hedef yükseklik.</param>
    /// <returns>FFmpeg <c>-vf</c> metni.</returns>
    public static string BuildFitFilter(int width, int height) =>
        string.Create(CultureInfo.InvariantCulture, $"scale={width}:{height}:force_original_aspect_ratio=increase:flags=lanczos,crop={width}:{height},setsar=1");

    /// <summary><c>1088x1920</c> biçimindeki ffprobe çıktısını çözer.</summary>
    /// <param name="text">Çıktı.</param>
    /// <param name="width">Genişlik.</param>
    /// <param name="height">Yükseklik.</param>
    /// <returns>Çözüldüyse <c>true</c>.</returns>
    public static bool TryParseSize(string? text, out int width, out int height)
    {
        width = 0;
        height = 0;
        var line = (text ?? string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        var parts = line?.Split('x');
        return parts is { Length: 2 }
            && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out width)
            && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out height)
            && width > 0 && height > 0;
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // Geçici dosya; silinemezse bir sonraki denemede üzerine yazılır.
        }
    }
}
