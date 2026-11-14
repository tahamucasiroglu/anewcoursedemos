using System.Security.Cryptography;
using System.Text.Json;
using TahaKonsept.Agent.Abstractions.Engines;
using TahaKonsept.Agent.Abstractions.Engines.Abstract;
using TahaKonsept.Agent.Core.Execution.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Agent;
using TahaKonsept.Shared.Contracts.Common;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Agent.Core.Execution.Base;

/// <summary>
/// Aşama yürütücü tabanı: <c>PayloadJson</c>'ı <typeparamref name="TPayload"/>'a çözer (<see cref="ContractJson"/>), doğrular,
/// aşama çıktı klasörünün girdi hash'ini denetler (değiştiyse eski çıktıları siler) ve <see cref="ExecuteCoreAsync"/>'ı çağırır.
/// Yardımcılar: <see cref="RunCheckpointedAsync"/> (önce <c>.partial</c> dosyasına üret, bitince taşı; varsa atla),
/// <see cref="CreateArtifact"/> (boyut + SHA256), <see cref="EnsureEngineReadyAsync"/>, <see cref="SelectByKey{T}"/>.
/// </summary>
/// <typeparam name="TPayload">Aşama girdi türü.</typeparam>
public abstract class StageExecutorBase<TPayload> : IStageExecutor
    where TPayload : class
{
    /// <inheritdoc />
    public abstract PipelineStage Stage { get; }

    /// <inheritdoc />
    public async Task<Result<StageExecutionOutput>> ExecuteAsync(StageExecutionContext context, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context);
        TPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<TPayload>(
                string.IsNullOrWhiteSpace(context.Command.PayloadJson) ? "{}" : context.Command.PayloadJson,
                ContractJson.Options);
        }
        catch (JsonException ex)
        {
            return Error.Validation("agent.invalid_payload", Stage + " aşama girdisi okunamadı: " + ex.Message);
        }

        if (payload is null)
        {
            return Error.Validation("agent.invalid_payload", Stage + " aşama girdisi boş.");
        }

        var validation = Validate(payload, context);
        if (validation.IsFailure)
        {
            return validation.Error;
        }

        PrepareOutputDirectory(GetOutputDirectory(context, payload), context.Command.InputHash);
        return await ExecuteCoreAsync(context, payload, ct);
    }

    /// <summary>Aşamanın çıktı klasörü (girdi hash'i bu klasör için tutulur).</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="payload">Girdi.</param>
    /// <returns>Klasör.</returns>
    protected abstract string GetOutputDirectory(StageExecutionContext context, TPayload payload);

    /// <summary>Girdiyi doğrular (varsayılan: geçerli).</summary>
    /// <param name="payload">Girdi.</param>
    /// <param name="context">Bağlam.</param>
    /// <returns>Sonuç.</returns>
    protected virtual Result Validate(TPayload payload, StageExecutionContext context) => Result.Success();

    /// <summary>Aşamanın asıl işi.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="payload">Girdi.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    protected abstract Task<Result<StageExecutionOutput>> ExecuteCoreAsync(StageExecutionContext context, TPayload payload, CancellationToken ct);

    /// <summary>
    /// Kontrol noktalı üretim: <paramref name="finalPath"/> zaten varsa atlar; yoksa <c>.partial</c> yolunu üreticiye verir,
    /// üretici gerçek çıktı yolunu döner, dosya son konuma taşınır. İptal/hata durumunda yarım dosya kalıcı olmaz.
    /// </summary>
    /// <param name="finalPath">Son dosya yolu.</param>
    /// <param name="produce">Geçici yolu alıp üretilen dosya yolunu dönen üretici.</param>
    /// <returns>Üretildiyse <c>true</c>, atlandıysa <c>false</c>; ya da hata.</returns>
    protected static async Task<Result<bool>> RunCheckpointedAsync(string finalPath, Func<string, Task<Result<string>>> produce)
    {
        ArgumentNullException.ThrowIfNull(produce);
        if (IsComplete(finalPath))
        {
            return false;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
        var partial = PartialPath(finalPath);
        TryDelete(partial);
        try
        {
            var produced = await produce(partial);
            if (produced.IsFailure)
            {
                return produced.Error;
            }

            var outputPath = string.IsNullOrWhiteSpace(produced.Value) ? partial : produced.Value;
            if (!File.Exists(outputPath))
            {
                return Error.Failure("agent.output_missing", "Beklenen çıktı dosyası oluşmadı: " + Path.GetFileName(finalPath));
            }

            File.Move(outputPath, finalPath, overwrite: true);
            return true;
        }
        finally
        {
            TryDelete(partial);
        }
    }

    /// <summary>Dosya var ve boş değil mi.</summary>
    /// <param name="path">Yol.</param>
    /// <returns>Tamamsa <c>true</c>.</returns>
    protected static bool IsComplete(string path) => File.Exists(path) && new FileInfo(path).Length > 0;

    /// <summary>Son dosyanın geçici adı (<c>ad.partial.uzantı</c>; motorlar uzantıdan biçim çıkarabilsin diye uzantı korunur).</summary>
    /// <param name="finalPath">Son yol.</param>
    /// <returns>Geçici yol.</returns>
    protected static string PartialPath(string finalPath) =>
        Path.Combine(Path.GetDirectoryName(finalPath) ?? string.Empty, Path.GetFileNameWithoutExtension(finalPath) + ".partial" + Path.GetExtension(finalPath));

    /// <summary>Artefakt meta verisi üretir (boyut, SHA256, iş klasörüne göre göreli yol).</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="path">Dosya.</param>
    /// <param name="kind">Tür (<c>image</c>, <c>audio</c>, <c>video</c>, <c>json</c>).</param>
    /// <param name="sceneIndex">Sahne indeksi.</param>
    /// <param name="durationSec">Süre.</param>
    /// <param name="metadata">Ek meta veriler.</param>
    /// <returns>Artefakt.</returns>
    protected static ProducedArtifact CreateArtifact(
        StageExecutionContext context,
        string path,
        string kind,
        int? sceneIndex,
        double? durationSec,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        var info = new FileInfo(path);
        string sha;
        using (var stream = info.OpenRead())
        {
            sha = Convert.ToHexStringLower(SHA256.HashData(stream));
        }

        var fullPath = Path.GetFullPath(path);
        var jobDir = Path.GetFullPath(context.JobDir);
        var local = fullPath.StartsWith(jobDir, StringComparison.OrdinalIgnoreCase)
            ? Path.GetRelativePath(jobDir, fullPath).Replace('\\', '/')
            : fullPath;
        return new ProducedArtifact
        {
            Name = info.Name,
            Kind = kind,
            SceneIndex = sceneIndex,
            LocalPath = local,
            SizeBytes = info.Length,
            DurationSec = durationSec,
            Sha256 = sha,
            Metadata = metadata ?? new Dictionary<string, string>(),
        };
    }

    /// <summary>Motor kurulu değilse kurar; ilerlemeyi aşama yüzdesinde <paramref name="fromPercent"/>–<paramref name="toPercent"/> aralığına çevirir.</summary>
    /// <param name="engine">Motor.</param>
    /// <param name="context">Bağlam.</param>
    /// <param name="fromPercent">Başlangıç yüzdesi.</param>
    /// <param name="toPercent">Bitiş yüzdesi.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    protected static async Task<Result> EnsureEngineReadyAsync(IAgentEngine engine, StageExecutionContext context, double fromPercent, double toPercent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(context);
        if (engine.IsInstalled())
        {
            return Result.Success();
        }

        context.Report(fromPercent, engine.DisplayName + " hazırlanıyor…");
        var progress = new InlineProgress<EngineSetupProgress>(p =>
            context.Report(fromPercent + ((toPercent - fromPercent) * (p.Percent ?? 0d) / 100d), p.Message));
        var ready = await engine.EnsureReadyAsync(progress, ct);
        return ready.IsFailure
            ? Error.Failure("agent.engine_setup_failed", engine.DisplayName + " kurulamadı: " + ready.Error.Message)
            : Result.Success();
    }

    /// <summary>Anahtara göre öğe seçer (büyük/küçük harf duyarsız); anahtar boşsa ilk öğe.</summary>
    /// <typeparam name="T">Öğe türü.</typeparam>
    /// <param name="items">Öğeler.</param>
    /// <param name="key">Anahtar.</param>
    /// <param name="keySelector">Öğenin anahtarı.</param>
    /// <returns>Öğe ya da <c>null</c>.</returns>
    protected static T? SelectByKey<T>(IEnumerable<T> items, string? key, Func<T, string> keySelector)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(keySelector);
        return string.IsNullOrWhiteSpace(key)
            ? items.FirstOrDefault()
            : items.FirstOrDefault(i => string.Equals(keySelector(i), key, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Ortak JSON ayarlarıyla serileştirir.</summary>
    /// <typeparam name="T">Tür.</typeparam>
    /// <param name="value">Değer.</param>
    /// <returns>JSON.</returns>
    protected static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ContractJson.Options);

    /// <summary>Dosya adındaki geçersiz karakterleri ve yol ayraçlarını temizler.</summary>
    /// <param name="name">Ad.</param>
    /// <param name="fallback">Boş kalırsa kullanılacak ad.</param>
    /// <returns>Güvenli ad.</returns>
    protected static string SafeFileName(string? name, string fallback = "dosya") => FileNameSanitizer.Sanitize(name, fallback);

    private static void PrepareOutputDirectory(string directory, string inputHash)
    {
        Directory.CreateDirectory(directory);
        if (string.IsNullOrWhiteSpace(inputHash))
        {
            return;
        }

        var marker = Path.Combine(directory, WorkspaceLayout.InputHashFile);
        if (File.Exists(marker))
        {
            var previous = File.ReadAllText(marker).Trim();
            if (string.Equals(previous, inputHash, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(directory))
            {
                TryDelete(file);
            }

            foreach (var dir in Directory.GetDirectories(directory))
            {
                try
                {
                    Directory.Delete(dir, recursive: true);
                }
                catch (IOException)
                {
                    // Kilitli alt klasör; üzerine yazılır.
                }
            }
        }

        File.WriteAllText(marker, inputHash);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Kilitli; bir sonraki denemede üzerine yazılır.
        }
        catch (UnauthorizedAccessException)
        {
            // Salt okunur; bir sonraki denemede üzerine yazılır.
        }
    }
}
