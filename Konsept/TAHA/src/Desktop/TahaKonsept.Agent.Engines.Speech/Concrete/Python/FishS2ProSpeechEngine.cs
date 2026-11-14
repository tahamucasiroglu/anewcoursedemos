using System.IO.Compression;
using TahaKonsept.Agent.Abstractions.Engines;
using TahaKonsept.Agent.Engines.Speech.Base;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Speech.Concrete.Python;

/// <summary>
/// Fish Audio S2 Pro (FISH AUDIO RESEARCH LICENSE, ticari değil; HF <c>fishaudio/s2-pro</c>, kod <c>fishaudio/fish-speech</c>).
/// Çok dilli, referans sesle klonlama, en az 12 GB VRAM. Kurulumda fish-speech kaynak arşivi <c>PythonEnvsDir/fish-s2-pro/src</c>
/// altına indirilip açılır ve oradan kurulur; adapter <c>tools/api_server.py</c>'yi yerel portta çalıştırıp <c>/v1/tts</c> çağırır.
/// </summary>
public sealed class FishS2ProSpeechEngine : PythonSpeechEngineBase
{
    /// <summary>fish-speech kaynak arşivi.</summary>
    public static readonly Uri SourceArchive = new("https://github.com/fishaudio/fish-speech/archive/refs/heads/main.zip");

    /// <summary>Motoru kurar.</summary>
    /// <param name="services">Ortak bağımlılıklar.</param>
    public FishS2ProSpeechEngine(SpeechEngineServices services)
        : base(services)
    {
    }

    /// <inheritdoc />
    public override string Key => "fish-s2-pro";

    /// <inheritdoc />
    public override string DisplayName => "Fish S2 Pro";

    /// <inheritdoc />
    public override int? MinVramMb => 12288;

    /// <inheritdoc />
    public override string LanguageLabel => "Çok dilli, klonlama";

    /// <inheritdoc />
    public override IReadOnlyList<string> Languages { get; } = [];

    /// <inheritdoc />
    public override bool SupportsCloning => true;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, string> Voices { get; } = new Dictionary<string, string> { ["default"] = "Varsayılan ses" };

    /// <inheritdoc />
    public override string AdapterName => "fish";

    /// <inheritdoc />
    public override string? ModelId => "fishaudio/s2-pro";

    /// <inheritdoc />
    public override string? PythonVersion => "3.12";

    /// <inheritdoc />
    public override IReadOnlyList<string> Requirements => ["torch==2.8.0", "torchaudio==2.8.0", SourceDir, "huggingface_hub"];

    /// <inheritdoc />
    public override string? TorchIndexUrl => TorchCu128Index;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, object?> LoadOptions => new Dictionary<string, object?> { ["repo_dir"] = SourceDir };

    /// <summary>Açılmış fish-speech kaynak klasörü.</summary>
    public string SourceDir => Path.Combine(EnvironmentDir, "src", "fish-speech-main");

    /// <inheritdoc />
    protected override async Task<Result> PrepareSourcesAsync(IProgress<EngineSetupProgress>? progress, CancellationToken ct)
    {
        if (File.Exists(Path.Combine(SourceDir, "tools", "api_server.py")))
        {
            return Result.Success();
        }

        var archive = Path.Combine(EnvironmentDir, "fish-speech-main.zip");
        using var client = Services.HttpClientFactory.CreateClient(SpeechEngineServices.HttpClientName);
        var download = await DownloadFileAsync(
            client,
            SourceArchive,
            archive,
            f => progress?.Report(new EngineSetupProgress($"{DisplayName}: fish-speech kaynağı indiriliyor", Math.Round(5 + (f * 5), 1))),
            ct).ConfigureAwait(false);
        if (download.IsFailure)
        {
            return download;
        }

        try
        {
            var sourceRoot = Path.Combine(EnvironmentDir, "src");
            Directory.CreateDirectory(sourceRoot);
            await ZipFile.ExtractToDirectoryAsync(archive, sourceRoot, overwriteFiles: true, ct).ConfigureAwait(false);
            File.Delete(archive);
            return Result.Success();
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            return Result.Failure(SpeechErrors.SetupFailed(DisplayName, ex.Message));
        }
    }
}
