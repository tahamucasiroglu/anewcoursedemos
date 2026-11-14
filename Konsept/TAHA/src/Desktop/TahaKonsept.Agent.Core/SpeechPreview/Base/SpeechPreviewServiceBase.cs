using System.Globalization;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Abstractions.Speech;
using TahaKonsept.Agent.Abstractions.Speech.Abstract;
using TahaKonsept.Agent.Core.Execution;
using TahaKonsept.Agent.Core.SpeechPreview.Abstract;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Agent.Core.SpeechPreview.Base;

/// <summary>
/// Ses önizleme tabanı: motoru <c>EngineId</c> ile seçer, ayarlardan dil (<c>language</c>, varsayılan <c>tr</c>) ve hız (<c>speed</c>) okur,
/// 1 günden eski önizlemeleri temizler. Önizleme metni en fazla <see cref="MaxTextLength"/> karakterdir.
/// </summary>
public abstract class SpeechPreviewServiceBase : ISpeechPreviewService
{
    /// <summary>Önizleme metni üst sınırı.</summary>
    public const int MaxTextLength = 500;

    private readonly IEnumerable<ISpeechEngine> _engines;
    private readonly string _directory;

    /// <summary>Oluşturur.</summary>
    /// <param name="engines">Ses motorları.</param>
    /// <param name="paths">Ajan klasörleri.</param>
    protected SpeechPreviewServiceBase(IEnumerable<ISpeechEngine> engines, IAgentPaths paths)
    {
        _engines = engines;
        _directory = Path.Combine(paths.WorkspaceDir, WorkspaceLayout.PreviewsDir);
    }

    /// <inheritdoc />
    public async Task<SpeechPreviewResult> CreatePreviewAsync(SpeechPreviewCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        var engine = _engines.FirstOrDefault(e => string.Equals(e.Key, command.EngineId, StringComparison.OrdinalIgnoreCase));
        if (engine is null)
        {
            return Fail(command, "'" + command.EngineId + "' ses motoru bu bilgisayarda yok.");
        }

        if (string.IsNullOrWhiteSpace(command.Text))
        {
            return Fail(command, "Önizleme metni boş.");
        }

        try
        {
            Directory.CreateDirectory(_directory);
            CleanupOld();
            var ready = engine.IsInstalled() ? null : await engine.EnsureReadyAsync(null, ct);
            if (ready is { IsFailure: true })
            {
                return Fail(command, engine.DisplayName + " hazır değil: " + ready.Error.Message);
            }

            var fileName = command.RequestId.ToString("N") + ".wav";
            var output = Path.Combine(_directory, fileName);
            var text = command.Text.Length > MaxTextLength ? command.Text[..MaxTextLength] : command.Text;
            var language = command.Settings.TryGetValue("language", out var lang) && !string.IsNullOrWhiteSpace(lang) ? lang : "tr";
            var speed = command.Settings.TryGetValue("speed", out var s) && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && parsed > 0 ? parsed : 1.0;
            var result = await engine.SynthesizeAsync(new SpeechRequest(text, language, command.VoiceId, null, speed, output), ct);
            if (result.IsFailure)
            {
                return Fail(command, result.Error.Message);
            }

            if (!string.Equals(Path.GetFullPath(result.Value.AudioPath), Path.GetFullPath(output), StringComparison.OrdinalIgnoreCase))
            {
                File.Move(result.Value.AudioPath, output, overwrite: true);
            }

            return new SpeechPreviewResult { RequestId = command.RequestId, Success = true, AudioPath = "/previews/" + fileName };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            OnPreviewFailed(command, ex);
            return Fail(command, "Önizleme üretilemedi: " + ex.Message);
        }
    }

    /// <inheritdoc />
    public string? ResolvePreviewFile(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || !fileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var id = Path.GetFileNameWithoutExtension(fileName);
        if (!Guid.TryParseExact(id, "N", out _))
        {
            return null;
        }

        var path = Path.Combine(_directory, id + ".wav");
        return File.Exists(path) ? path : null;
    }

    /// <summary>Beklenmeyen önizleme hatasında çağrılır.</summary>
    /// <param name="command">Komut.</param>
    /// <param name="exception">İstisna.</param>
    protected virtual void OnPreviewFailed(SpeechPreviewCommand command, Exception exception)
    {
    }

    private static SpeechPreviewResult Fail(SpeechPreviewCommand command, string message) =>
        new() { RequestId = command.RequestId, Success = false, ErrorMessage = message };

    private void CleanupOld()
    {
        var threshold = DateTime.UtcNow.AddDays(-1);
        foreach (var file in Directory.EnumerateFiles(_directory))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(file) < threshold)
                {
                    File.Delete(file);
                }
            }
            catch (IOException)
            {
                // Kullanımda; sonra silinir.
            }
        }
    }
}
