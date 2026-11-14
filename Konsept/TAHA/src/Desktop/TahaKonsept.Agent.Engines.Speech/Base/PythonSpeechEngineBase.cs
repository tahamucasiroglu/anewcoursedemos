using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Abstractions.Engines;
using TahaKonsept.Agent.Abstractions.Processes;
using TahaKonsept.Agent.Abstractions.Speech;
using TahaKonsept.Agent.Abstractions.Tools;
using TahaKonsept.Agent.Engines.Speech.Python.Abstract;
using TahaKonsept.Agent.Engines.Speech.Python.Concrete;
using TahaKonsept.Agent.Engines.Speech.Python.Protocol;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Speech.Base;

/// <summary>
/// Python tabanlı ses motorlarının tabanı. Kurulum (<see cref="EnsureReadyAsync"/>): <c>uv</c> ile motor başına venv
/// (<c>PythonEnvsDir/&lt;Key&gt;/.venv</c>, Python <see cref="PythonVersion"/>; uv'nin yönettiği Python <c>ToolsDir/uv-python</c> altına iner),
/// sabitlenmiş <see cref="Requirements"/> kurulumu (GPU motorlarında <see cref="TorchIndexUrl"/> CUDA tekerlek dizini), başarıda
/// gereksinim parmak izini <c>.taha-installed</c> dosyasına yazar (<see cref="IsInstalled"/> bunu karşılaştırır).
/// Çalışma: <c>PythonWorkers/tts_worker.py</c> uzun ömürlü süreç olarak açılır, ilk istekte <c>load</c> (adapter <see cref="AdapterName"/>,
/// <see cref="ModelId"/>, <see cref="Device"/>, <see cref="LoadOptions"/>), her istekte <c>synth</c>. İşçi çökerse sonraki istekte yeniden başlar.
/// Model dosyaları Hugging Face önbelleği olarak <c>ModelsDir/huggingface</c> altına iner (HF_HOME).
/// </summary>
public abstract class PythonSpeechEngineBase : SpeechEngineBase, IAsyncDisposable, IDisposable
{
    /// <summary>Kurulum işaret dosyası adı.</summary>
    public const string InstalledMarkerFile = ".taha-installed";

    /// <summary>CUDA 12.4 PyTorch tekerlek dizini (torch 2.6 için).</summary>
    protected const string TorchCu124Index = "https://download.pytorch.org/whl/cu124";

    /// <summary>CUDA 12.8 PyTorch tekerlek dizini (torch 2.7+ için).</summary>
    protected const string TorchCu128Index = "https://download.pytorch.org/whl/cu128";

    private readonly SemaphoreSlim _workerGate = new(1, 1);
    private ITtsWorkerClient? _worker;
    private bool _disposed;

    /// <summary>Ortak bağımlılıklarla kurar.</summary>
    /// <param name="services">Ortak bağımlılıklar.</param>
    protected PythonSpeechEngineBase(SpeechEngineServices services)
        : base(services)
    {
    }

    /// <summary>Python adapter modülü (<c>PythonWorkers/adapters/&lt;ad&gt;.py</c>).</summary>
    public abstract string AdapterName { get; }

    /// <summary>Adapter'a verilen model kimliği / HF deposu (adapter varsayılanı için <c>null</c>).</summary>
    public virtual string? ModelId => null;

    /// <summary>Sabitlenmiş pip gereksinimleri (uv pip install argümanları).</summary>
    public abstract IReadOnlyList<string> Requirements { get; }

    /// <summary>PyTorch CUDA tekerlek dizini (ek dizin olarak eklenir); CPU motorlarında <c>null</c>.</summary>
    public virtual string? TorchIndexUrl => null;

    /// <summary>venv Python sürümü (uv indirir); <c>null</c> ise bulunan gömülü Python (ToolKind.Python) ya da uv varsayılanı.</summary>
    public virtual string? PythonVersion => "3.11";

    /// <summary>Cihaz: GPU gerektiren motorlarda <c>cuda</c> (adapter GPU yoksa CPU'ya düşer), diğerlerinde <c>cpu</c>.</summary>
    public virtual string Device => MinVramMb is null ? "cpu" : "cuda";

    /// <summary><c>load</c> komutuna giden ek seçenekler.</summary>
    public virtual IReadOnlyDictionary<string, object?> LoadOptions => new Dictionary<string, object?>();

    /// <summary>Model yükleme zaman aşımı (ilk yüklemede çok GB indirme olabilir).</summary>
    public virtual TimeSpan LoadTimeout => TimeSpan.FromMinutes(60);

    /// <summary>Tek sentez zaman aşımı.</summary>
    public virtual TimeSpan SynthTimeout => TimeSpan.FromMinutes(15);

    /// <summary>Paket kurulum zaman aşımı.</summary>
    public virtual TimeSpan InstallTimeout => TimeSpan.FromMinutes(90);

    /// <summary>Motorun ortam klasörü: <c>PythonEnvsDir/&lt;Key&gt;</c>.</summary>
    public string EnvironmentDir => Path.Combine(Services.Paths.PythonEnvsDir, Key);

    /// <summary>venv klasörü.</summary>
    public string VenvDir => Path.Combine(EnvironmentDir, ".venv");

    /// <summary>venv Python yorumlayıcısı.</summary>
    public string VenvPython => OperatingSystem.IsWindows()
        ? Path.Combine(VenvDir, "Scripts", "python.exe")
        : Path.Combine(VenvDir, "bin", "python");

    /// <summary><c>tts_worker.py</c> yolu (çıktı klasöründeki <c>PythonWorkers</c>).</summary>
    public virtual string WorkerScriptPath => Path.Combine(AppContext.BaseDirectory, "PythonWorkers", "tts_worker.py");

    /// <summary>Gereksinim parmak izi (Python sürümü + dizin + paketler); değişirse yeniden kurulur.</summary>
    public string RequirementsFingerprint
    {
        get
        {
            var text = string.Join('\n', new[] { PythonVersion ?? "-", TorchIndexUrl ?? "-" }.Concat(Requirements));
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        }
    }

    /// <inheritdoc />
    public override bool IsInstalled()
    {
        var marker = Path.Combine(EnvironmentDir, InstalledMarkerFile);
        return File.Exists(VenvPython) && File.Exists(marker) &&
               string.Equals(File.ReadAllText(marker).Trim(), RequirementsFingerprint, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override async Task<Result> EnsureReadyAsync(IProgress<EngineSetupProgress>? progress, CancellationToken ct)
    {
        if (IsInstalled())
        {
            return Result.Success();
        }

        var uv = Services.Tools.Require(ToolKind.Uv);
        if (uv.IsFailure)
        {
            return uv;
        }

        await StopWorkerAsync().ConfigureAwait(false);
        Directory.CreateDirectory(EnvironmentDir);
        var environment = UvEnvironment();

        progress?.Report(new EngineSetupProgress($"{DisplayName}: Python ortamı hazırlanıyor", 5));
        var pythonArgument = PythonVersion ?? Services.Tools.Find(ToolKind.Python);
        var venvArgs = new List<string> { "venv", VenvDir, "--allow-existing" };
        if (!string.IsNullOrWhiteSpace(pythonArgument))
        {
            venvArgs.AddRange(["--python", pythonArgument]);
        }

        var venv = await RunUvAsync(uv.Value, venvArgs, environment, TimeSpan.FromMinutes(15), progress, 5, ct).ConfigureAwait(false);
        if (venv.IsFailure)
        {
            return venv;
        }

        var prepared = await PrepareSourcesAsync(progress, ct).ConfigureAwait(false);
        if (prepared.IsFailure)
        {
            return prepared;
        }

        if (Requirements.Count > 0)
        {
            progress?.Report(new EngineSetupProgress($"{DisplayName}: paketler kuruluyor (birkaç GB olabilir)", 15));
            var installArgs = new List<string> { "pip", "install", "--python", VenvPython };
            if (!string.IsNullOrWhiteSpace(TorchIndexUrl))
            {
                installArgs.AddRange(["--extra-index-url", TorchIndexUrl, "--index-strategy", "unsafe-best-match"]);
            }

            installArgs.AddRange(Requirements);
            var install = await RunUvAsync(uv.Value, installArgs, environment, InstallTimeout, progress, 15, ct).ConfigureAwait(false);
            if (install.IsFailure)
            {
                return install;
            }
        }

        await File.WriteAllTextAsync(Path.Combine(EnvironmentDir, InstalledMarkerFile), RequirementsFingerprint, ct).ConfigureAwait(false);
        progress?.Report(new EngineSetupProgress($"{DisplayName}: kurulum tamam (model ilk kullanımda indirilir)", 100));
        return Result.Success();
    }

    /// <summary>Çalışan işçiyle ping atar (tanılama).</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Başarı ya da hata.</returns>
    public async Task<Result> PingAsync(CancellationToken ct)
    {
        var worker = await GetWorkerAsync(ct).ConfigureAwait(false);
        if (worker.IsFailure)
        {
            return worker;
        }

        var response = await worker.Value.SendAsync(WorkerProtocol.Ping(), TimeSpan.FromSeconds(30), ct).ConfigureAwait(false);
        return response.IsFailure ? response : response.Value.Ok ? Result.Success() : Result.Failure(SpeechErrors.Worker(response.Value.Error ?? "ping"));
    }

    /// <summary>İşçi sürecini kapatır (model bellekten çıkar).</summary>
    /// <returns>Görev.</returns>
    public async Task StopWorkerAsync()
    {
        await _workerGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_worker is not null)
            {
                await _worker.DisposeAsync().ConfigureAwait(false);
                _worker = null;
            }
        }
        finally
        {
            _workerGate.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await StopWorkerAsync().ConfigureAwait(false);
        _workerGate.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Eşzamanlı kapatma (DI kabı senkron dispose ederse): işçiyi durdurur.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopWorkerAsync().GetAwaiter().GetResult();
        _workerGate.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    protected override async Task<Result<SpeechOutput>> SynthesizeCoreAsync(SpeechRequest request, CancellationToken ct)
    {
        var worker = await GetWorkerAsync(ct).ConfigureAwait(false);
        if (worker.IsFailure)
        {
            return Result.Failure<SpeechOutput>(worker.Error);
        }

        var output = Path.GetFullPath(request.OutputPath);
        var synth = WorkerProtocol.Synth(request.Text, NormalizeLanguage(request.Language), request.VoiceId, request.ReferenceAudioPath, request.Speed, output);
        var response = await worker.Value.SendAsync(synth, SynthTimeout, ct).ConfigureAwait(false);
        if (response.IsFailure)
        {
            return Result.Failure<SpeechOutput>(response.Error);
        }

        if (!response.Value.Ok)
        {
            return Result.Failure<SpeechOutput>(SpeechErrors.SynthesisFailed(DisplayName, response.Value.Error ?? "bilinmeyen hata"));
        }

        if (!File.Exists(output))
        {
            return Result.Failure<SpeechOutput>(SpeechErrors.SynthesisFailed(DisplayName, "çıktı dosyası oluşmadı."));
        }

        var duration = ReadDurationSec(output) ?? response.Value.Duration ?? 0;
        return Result.Success(new SpeechOutput(output, duration));
    }

    /// <summary>Paket kurulumundan önce çalışan ek hazırlık (ör. kaynak deposu indirme). Varsayılan: hiçbir şey.</summary>
    /// <param name="progress">İlerleme.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Başarı ya da hata.</returns>
    protected virtual Task<Result> PrepareSourcesAsync(IProgress<EngineSetupProgress>? progress, CancellationToken ct) =>
        Task.FromResult(Result.Success());

    /// <summary>İşçi sürecinin ortam değişkenleri (HF önbelleği ModelsDir altında).</summary>
    /// <returns>Ortam değişkenleri.</returns>
    protected virtual IReadOnlyDictionary<string, string> WorkerEnvironment() => new Dictionary<string, string>
    {
        ["HF_HOME"] = Path.Combine(Services.Paths.ModelsDir, "huggingface"),
        ["HF_HUB_DISABLE_TELEMETRY"] = "1",
        ["COQUI_TOS_AGREED"] = "1",
        ["TAHA_ENGINE_KEY"] = Key,
    };

    /// <summary>İşçi süreci oluşturur (testlerde farklı taşıma için ezilebilir).</summary>
    /// <returns>İstemci.</returns>
    protected virtual ITtsWorkerClient CreateWorker() =>
        ProcessTtsWorkerClient.Start(VenvPython, WorkerScriptPath, WorkerEnvironment(), Logger);

    private async Task<Result<ITtsWorkerClient>> GetWorkerAsync(CancellationToken ct)
    {
        await _workerGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_worker is { IsRunning: true })
            {
                return Result.Success(_worker);
            }

            if (_worker is not null)
            {
                await _worker.DisposeAsync().ConfigureAwait(false);
                _worker = null;
            }

            if (!File.Exists(WorkerScriptPath))
            {
                return Result.Failure<ITtsWorkerClient>(SpeechErrors.Worker($"tts_worker.py bulunamadı: {WorkerScriptPath}"));
            }

            ITtsWorkerClient worker;
            try
            {
                worker = CreateWorker();
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or IOException)
            {
                return Result.Failure<ITtsWorkerClient>(SpeechErrors.Worker(ex.Message));
            }

            Result<WorkerResponse> load;
            try
            {
                load = await worker.SendAsync(WorkerProtocol.Load(AdapterName, ModelId, Device, LoadOptions), LoadTimeout, ct).ConfigureAwait(false);
            }
            catch
            {
                // Yükleme sırasında iptal (duraklat/iptal) ya da beklenmeyen hata: süreç öksüz kalıp modeli GPU belleğinde tutmasın.
                await worker.DisposeAsync().ConfigureAwait(false);
                throw;
            }

            if (load.IsFailure || !load.Value.Ok)
            {
                await worker.DisposeAsync().ConfigureAwait(false);
                var reason = load.IsFailure ? load.Error.Message : load.Value.Error ?? "model yüklenemedi";
                return Result.Failure<ITtsWorkerClient>(SpeechErrors.Worker($"{DisplayName} yüklenemedi: {reason}"));
            }

            Logger.LogInformation("{Engine} Python işçisinde yüklendi ({Device})", Key, Device);
            _worker = worker;
            return Result.Success(worker);
        }
        finally
        {
            _workerGate.Release();
        }
    }

    private Dictionary<string, string> UvEnvironment() => new()
    {
        ["UV_PYTHON_INSTALL_DIR"] = Path.Combine(Services.Paths.ToolsDir, "uv-python"),
        ["UV_LINK_MODE"] = "copy",
        ["UV_NO_PROGRESS"] = "1",
    };

    private async Task<Result> RunUvAsync(
        string uv,
        IReadOnlyList<string> arguments,
        IReadOnlyDictionary<string, string> environment,
        TimeSpan timeout,
        IProgress<EngineSetupProgress>? progress,
        double percent,
        CancellationToken ct)
    {
        Logger.LogInformation("uv {Args}", string.Join(' ', arguments));
        var result = await Services.Processes.RunAsync(
            new ProcessSpec(uv, arguments, EnvironmentDir, environment, timeout),
            line =>
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    progress?.Report(new EngineSetupProgress($"{DisplayName}: {line.Trim()}", percent));
                }
            },
            ct).ConfigureAwait(false);
        if (result.Succeeded)
        {
            return Result.Success();
        }

        var reason = result.TimedOut ? "zaman aşımı" : LastLines(result.StandardError + "\n" + result.StandardOutput, 8);
        Logger.LogError("uv başarısız ({Code}): {Reason}", result.ExitCode, reason);
        return Result.Failure(SpeechErrors.SetupFailed(DisplayName, reason));
    }

    private static string LastLines(string text, int count) =>
        string.Join(" | ", text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).TakeLast(count));
}
