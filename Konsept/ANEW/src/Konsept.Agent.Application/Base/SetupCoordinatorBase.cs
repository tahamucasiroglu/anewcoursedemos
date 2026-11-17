using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Domain.Rules;

namespace Konsept.Agent.Application.Base;

/// <summary>
/// Kurulum motoru (spec 0009 Req 5, 10–13, 15, 17, 19–20). Bileşenler sırayla kurulur: Bekliyor → İndiriliyor → Doğrulanıyor →
/// Kuruluyor → Kuruldu. İndirme <c>downloads\</c>'a yapılır; başarısız indirme 2 kez kendiliğinden yeniden denenir, sonra (varsa) Pi
/// aynasından denenir. SHA-256 uyuşmayan dosya kurulmaz. Arşiv <c>components\&lt;id&gt;.partial</c>'a açılır, sonra
/// <c>components\&lt;id&gt;</c>'ye taşınır; yarım kalan her şey açılışta ve iptalde silinir, Kuruldu olanlar korunur. Kurulumda
/// yürütülebilir dosyanın hash'i durum dosyasına yazılır, her açılışta karşılaştırılır (uyuşmazsa Onarılmalı). Bir bileşenin hatası
/// diğerlerini durdurmaz. Teknik log alt sınıftadır.
/// </summary>
public abstract class SetupCoordinatorBase : ISetupCoordinator, IProgress<double>
{
    private const int ErrorDiskFull = unchecked((int)0x80070070);
    private const int ErrorHandleDiskFull = unchecked((int)0x80070027);
    private readonly Lock _gate = new();
    private readonly IReadOnlyList<SetupComponent> _catalog;
    private readonly ISetupHost _host;
    private readonly ISetupStateStore _store;
    private readonly string _defaultVideoFolder;
    private readonly Uri? _mirrorBase;
    private readonly ComponentState[] _states;
    private readonly Dictionary<string, InstalledComponent> _installed = new(StringComparer.Ordinal);
    private string? _videoFolder;
    private SetupComponent? _current;
    private bool _running;
    private double _currentFraction;
    private double _spanStart;
    private double _spanWidth = 1;
    private int _percent;

    /// <summary>Initializes a new instance of the <see cref="SetupCoordinatorBase"/> class.</summary>
    /// <param name="catalog">Bileşen listesi (kurulum sırası).</param>
    /// <param name="host">Dış dünya.</param>
    /// <param name="store">Kalıcı durum.</param>
    /// <param name="installRoot">Kurulum yeri.</param>
    /// <param name="defaultVideoFolder">Varsayılan video klasörü.</param>
    /// <param name="mirrorBase">Pi aynasının adresi (<c>…/ajan-bilesenleri/</c>); yoksa <c>null</c>.</param>
    /// <param name="gpu">Tespit edilen ekran kartı (spec 0028) ya da <c>null</c>.</param>
    protected SetupCoordinatorBase(
        IReadOnlyList<SetupComponent> catalog,
        ISetupHost host,
        ISetupStateStore store,
        string installRoot,
        string defaultVideoFolder,
        Uri? mirrorBase,
        GpuInfo? gpu = null)
    {
        Gpu = gpu;
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        InstallRoot = installRoot ?? throw new ArgumentNullException(nameof(installRoot));
        _defaultVideoFolder = defaultVideoFolder ?? throw new ArgumentNullException(nameof(defaultVideoFolder));
        _mirrorBase = mirrorBase;
        _states = [.. _catalog.Select(c => new ComponentState(c, ComponentStatus.Waiting))];
    }

    /// <inheritdoc />
    public event EventHandler<SetupEvent>? Changed;

    /// <inheritdoc />
    public IReadOnlyList<ComponentState> Components
    {
        get
        {
            lock (_gate)
            {
                return [.. _states];
            }
        }
    }

    /// <inheritdoc />
    public string InstallRoot { get; }

    /// <inheritdoc />
    public GpuInfo? Gpu { get; }

    /// <inheritdoc />
    public string VideoFolder => _videoFolder ?? _defaultVideoFolder;

    /// <inheritdoc />
    public bool IsRunning => Volatile.Read(ref _running);

    /// <inheritdoc />
    public bool IsReady { get; private set; }

    /// <inheritdoc />
    public int Percent => Volatile.Read(ref _percent);

    private string ComponentsDirectory => Path.Combine(InstallRoot, "components");

    private string DownloadsDirectory => Path.Combine(InstallRoot, "downloads");

    /// <inheritdoc />
    public async Task<bool> InspectAsync(CancellationToken cancellationToken)
    {
        CleanPartials();
        var state = _store.Load();
        _videoFolder = state.VideoFolder;
        _installed.Clear();
        foreach (var (id, installed) in state.Installed)
        {
            _installed[id] = installed;
        }

        for (var i = 0; i < _states.Length; i++)
        {
            var component = _states[i].Component;

            // İsteğe bağlı bileşen (ör. yerel model) ilk kurulumda indirilmez; üretim isteyince onayla kurulur (spec 0027).
            var status = component.Required ? ComponentStatus.Waiting : ComponentStatus.Skipped;
            if (_installed.TryGetValue(component.Id, out var record) && string.Equals(record.Version, component.Version, StringComparison.Ordinal))
            {
                if (await IsIntactAsync(component, record, cancellationToken).ConfigureAwait(false))
                {
                    status = ComponentStatus.Installed;
                }
                else
                {
                    status = ComponentStatus.NeedsRepair;
                    WriteLog(AgentLogLevel.Warning, "Kurulum: bileşen onarılmalı (dosya değişmiş ya da eksik): {Component}", component.Name);
                }
            }

            SetState(new ComponentState(component, status), raise: false);
        }

        IsReady = _states.All(s => s.Status is ComponentStatus.Installed or ComponentStatus.Skipped);
        UpdatePercent();
        return !IsReady;
    }

    /// <inheritdoc />
    public SetupPreflight Preflight()
    {
        // Atlandı (isteğe bağlı, ör. yerel model) bileşenler ilk kurulumda inmez; alan hesabına girmez (spec 0028 Req 13).
        var pending = Components.Where(s => s.Status is not ComponentStatus.Installed and not ComponentStatus.Skipped).Select(s => s.Component);
        long free;
        try
        {
            Directory.CreateDirectory(InstallRoot);
            free = _host.FreeBytes(InstallRoot);
        }
        catch (IOException)
        {
            free = 0;
        }

        return new SetupPreflight(_host.IsOnline(), SetupRules.RequiredBytes(pending), free);
    }

    /// <inheritdoc />
    public bool TrySetVideoFolder(string folder)
    {
        if (!SetupRules.IsValidVideoFolder(folder, InstallRoot))
        {
            return false;
        }

        _videoFolder = Path.GetFullPath(folder);
        Persist();
        return true;
    }

    /// <inheritdoc />
    public Task RunAsync(CancellationToken cancellationToken) =>
        RunCoreAsync(s => s.Status is not ComponentStatus.Installed and not ComponentStatus.Skipped, cancellationToken);

    /// <inheritdoc />
    public Task RetryAsync(string componentId, CancellationToken cancellationToken) =>
        RunCoreAsync(s => string.Equals(s.Component.Id, componentId, StringComparison.Ordinal), cancellationToken);

    /// <inheritdoc />
    public async Task<bool> InstallAsync(IReadOnlyCollection<string> componentIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(componentIds);

        // Oturum sırasında bozulan (ör. karantinaya alınan) istenen bileşen Kurulu görünse de önce denetlenir; bozuksa onarılır (r2 Info-1).
        foreach (var state in Components.Where(s => componentIds.Contains(s.Component.Id, StringComparer.Ordinal) && s.Status == ComponentStatus.Installed))
        {
            if (!_installed.TryGetValue(state.Component.Id, out var record) || !await IsIntactAsync(state.Component, record, cancellationToken).ConfigureAwait(false))
            {
                SetState(new ComponentState(state.Component, ComponentStatus.NeedsRepair), raise: true);
            }
        }

        return await RunCoreAsync(s => componentIds.Contains(s.Component.Id, StringComparer.Ordinal) && s.Status != ComponentStatus.Installed, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Kurulum olayını teknik loga yazar (şifre/token içermez; yalnızca bileşen adları ve sebepler).</summary>
    /// <param name="level">Seviye.</param>
    /// <param name="messageTemplate">Mesaj şablonu.</param>
    /// <param name="values">Değerler.</param>
    protected abstract void WriteLog(AgentLogLevel level, string messageTemplate, params object?[] values);

    private static bool IsDiskFull(IOException exception) =>
        exception.HResult is ErrorDiskFull or ErrorHandleDiskFull;

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private static void DeleteFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private async Task<bool> RunCoreAsync(Func<ComponentState, bool> select, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_running)
            {
                return false;
            }

            _running = true;
        }

        IsReady = false;
        try
        {
            var targets = Components
                .Where(select)
                .Select(s => s.Component)
                .ToList();
            Raise(new SetupEvent(SetupEventKind.Started));
            WriteLog(AgentLogLevel.Information, "Kurulum başladı: {Components}", string.Join(", ", targets.Select(c => c.Name)));
            foreach (var component in targets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await InstallAsync(component, cancellationToken).ConfigureAwait(false);
            }

            if (Components.All(s => s.Status is ComponentStatus.Installed or ComponentStatus.Skipped))
            {
                await ReadyCheckAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                Raise(new SetupEvent(SetupEventKind.Stopped));
            }
        }
        catch (OperationCanceledException)
        {
            WriteLog(AgentLogLevel.Warning, "Kurulum durduruldu; yarım bileşenin dosyaları silindi, tamamlananlar korundu");
            Raise(new SetupEvent(SetupEventKind.Stopped));
        }
        finally
        {
            Volatile.Write(ref _running, false);
        }

        return true;
    }

    private async Task InstallAsync(SetupComponent component, CancellationToken cancellationToken)
    {
        var archive = Path.Combine(DownloadsDirectory, component.FileName);
        var partial = Path.Combine(ComponentsDirectory, component.Id + ".partial");
        var step = InstallStep.Download;
        try
        {
            _currentFraction = 0;
            (_spanStart, _spanWidth) = (0, 1);
            if (component.Installer == SetupComponent.Npm)
            {
                await InstallPackagesAsync(component, partial, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (component.Installer == SetupComponent.Python)
            {
                await InstallPythonAsync(component, partial, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (component.Installer == SetupComponent.FilesInstaller)
            {
                await InstallFilesAsync(component, partial, cancellationToken).ConfigureAwait(false);
                return;
            }

            SetStatus(component, ComponentStatus.Downloading);
            Directory.CreateDirectory(DownloadsDirectory);
            var failure = await DownloadAsync(component, archive, cancellationToken).ConfigureAwait(false);
            if (failure != SetupFailure.None)
            {
                Fail(component, failure, InstallStep.Download, "indirilemedi");
                return;
            }

            step = InstallStep.Verify;
            SetStatus(component, ComponentStatus.Verifying);
            var hash = await _host.ComputeSha256Async(archive, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(hash, component.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                DeleteFile(archive);
                Fail(component, SetupFailure.VerifyFailed, InstallStep.Verify, "doğrulanamadı (SHA-256 uyuşmadı)");
                return;
            }

            step = InstallStep.Install;
            SetStatus(component, ComponentStatus.Installing);
            var target = Path.Combine(ComponentsDirectory, component.Id);
            DeleteDirectory(partial);
            _host.ExtractArchive(archive, partial);
            var source = component.ArchiveRoot is null ? partial : Path.Combine(partial, component.ArchiveRoot);
            DeleteDirectory(target);
            Directory.Move(source, target);
            DeleteDirectory(partial);
            DeleteFile(archive);
            await RecordAsync(component, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            DeleteDirectory(partial);
            DeleteFile(archive);
            _currentFraction = 0;
            SetStatus(component, ComponentStatus.Waiting);
            throw;
        }
        catch (IOException exception) when (IsDiskFull(exception))
        {
            Cleanup(partial, archive);
            Fail(component, SetupFailure.DiskFull, step, "diskte yeterli yer yok");
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            Cleanup(partial, archive);
            WriteLog(AgentLogLevel.Warning, "Kurulum ayrıntısı: {Component} {ExceptionType}: {ExceptionMessage}", component.Name, exception.GetType().Name, exception.Message);
            Fail(component, SetupFailure.InstallFailed, step, "kurulamadı");
        }
    }

    /// <summary>Kurulan bileşenin yürütülebilir dosyasını doğrular ve kaydeder.</summary>
    private async Task RecordAsync(SetupComponent component, CancellationToken cancellationToken)
    {
        var executable = ExecutablePath(component);
        if (!File.Exists(executable))
        {
            Fail(component, SetupFailure.InstallFailed, InstallStep.Install, "kurulamadı (yürütülebilir dosya yok)");
            return;
        }

        var executableHash = await _host.ComputeSha256Async(executable, cancellationToken).ConfigureAwait(false);
        _installed[component.Id] = new InstalledComponent(component.Version, executableHash);
        Persist();
        _currentFraction = 0;
        SetStatus(component, ComponentStatus.Installed);
        WriteLog(AgentLogLevel.Information, "Kurulum: bileşen kuruldu: {Component} {Version}", component.Name, component.Version);
    }

    /// <summary>
    /// npm bileşeni (spec 0018 / B-118: HyperFrames): indirme/arşiv adımı yok; ajana gömülü kilit dosyasıyla kurulu Node'un npm'i kurar
    /// (paket bütünlüğü npm <c>integrity</c> ile). Önce Node kurulu olmalı (liste sırası).
    /// </summary>
    private async Task InstallPackagesAsync(SetupComponent component, string partial, CancellationToken cancellationToken)
    {
        SetStatus(component, ComponentStatus.Installing);
        DeleteDirectory(partial);
        Directory.CreateDirectory(partial);
        if (!await _host.InstallPackagesAsync(component.Id, component.Sha256, Path.Combine(ComponentsDirectory, "node"), partial, cancellationToken).ConfigureAwait(false))
        {
            DeleteDirectory(partial);
            Fail(component, SetupFailure.InstallFailed, InstallStep.Install, "kurulamadı");
            return;
        }

        var target = Path.Combine(ComponentsDirectory, component.Id);
        DeleteDirectory(target);
        Directory.Move(partial, target);
        await RecordAsync(component, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Python paket ortamı (spec 0028 Req 4): indirme/arşiv adımı yok; kurulu taşınabilir Python ve uv ile ajana gömülü hash'li kilit dosyasından
    /// kurulur (bağımlılıklar liste sırasında önce kurulur). uv önbelleği <c>downloads/uv-cache</c>'tedir ve kurulumdan sonra silinir.
    /// </summary>
    private async Task InstallPythonAsync(SetupComponent component, string partial, CancellationToken cancellationToken)
    {
        SetStatus(component, ComponentStatus.Installing);
        var python = _catalog.FirstOrDefault(c => c.Id == SetupComponent.PythonRuntimeId);
        var uv = _catalog.FirstOrDefault(c => c.Id == SetupComponent.UvId);
        if (python is null || uv is null || !_installed.ContainsKey(python.Id) || !_installed.ContainsKey(uv.Id))
        {
            Fail(component, SetupFailure.InstallFailed, InstallStep.Install, "kurulamadı (Python ya da uv kurulu değil)");
            return;
        }

        DeleteDirectory(partial);
        Directory.CreateDirectory(partial);
        Directory.CreateDirectory(DownloadsDirectory);
        var ok = await _host.InstallPythonPackagesAsync(
            component.Id,
            component.Variant,
            component.Sha256,
            ExecutablePath(uv),
            ExecutablePath(python),
            partial,
            Path.Combine(DownloadsDirectory, "uv-cache"),
            cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            DeleteDirectory(partial);
            Fail(component, SetupFailure.InstallFailed, InstallStep.Install, "kurulamadı");
            return;
        }

        var target = Path.Combine(ComponentsDirectory, component.Id);
        DeleteDirectory(target);
        Directory.Move(partial, target);
        await RecordAsync(component, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Çok dosyalı bileşen (spec 0028 Req 3): önce listenin hash'i katalogdakiyle karşılaştırılır; her dosya <c>components/&lt;id&gt;.partial</c>'a
    /// iner, boyutu ve SHA-256'sı denetlenir (aynı denemede doğrulanmış dosya yeniden inmez). Hepsi doğrulanınca dosya listesi yazılır ve klasör
    /// yerine taşınır. Uyuşmazlıkta bozuk dosya silinir, bileşen Hata olur.
    /// </summary>
    private async Task InstallFilesAsync(SetupComponent component, string partial, CancellationToken cancellationToken)
    {
        var files = component.Files ?? [];
        if (files.Count == 0 || !string.Equals(ComponentRules.FilesManifestSha256(files), component.Sha256, StringComparison.OrdinalIgnoreCase)
            || !files.All(f => ComponentRules.IsSafeRelativePath(f.Path)))
        {
            Fail(component, SetupFailure.VerifyFailed, InstallStep.Verify, "doğrulanamadı (dosya listesi uyuşmadı)");
            return;
        }

        _current = component;
        SetStatus(component, ComponentStatus.Verifying); // Yeniden denemede var olan GB'lık dosyalar önce doğrulanır (review r1 L-3).
        var total = (double)Math.Max(1, files.Sum(f => f.Bytes));
        long done = 0;
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = Path.Combine(partial, file.Path.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            if (!await IsFileValidAsync(path, file, cancellationToken).ConfigureAwait(false))
            {
                SetStatus(component, ComponentStatus.Downloading);
                (_spanStart, _spanWidth) = (done / total, file.Bytes / total);
                var failure = await DownloadAsync(component, file.Url, path, this, mirror: false, cancellationToken).ConfigureAwait(false);
                (_spanStart, _spanWidth) = (0, 1);
                if (failure != SetupFailure.None)
                {
                    Fail(component, failure, InstallStep.Download, "indirilemedi");
                    return;
                }

                SetStatus(component, ComponentStatus.Verifying);
                if (!await IsFileValidAsync(path, file, cancellationToken).ConfigureAwait(false))
                {
                    DeleteFile(path);
                    Fail(component, SetupFailure.VerifyFailed, InstallStep.Verify, "doğrulanamadı (SHA-256 ya da boyut uyuşmadı)");
                    return;
                }
            }

            done += file.Bytes;
            ReportProgress(component, done / total);
        }

        SetStatus(component, ComponentStatus.Installing);
        await File.WriteAllTextAsync(Path.Combine(partial, SetupComponent.FilesManifestName), ComponentRules.FilesManifest(files), cancellationToken).ConfigureAwait(false);
        var target = Path.Combine(ComponentsDirectory, component.Id);
        DeleteDirectory(target);
        Directory.Move(partial, target);
        await RecordAsync(component, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> IsFileValidAsync(string path, ComponentFile file, CancellationToken cancellationToken) =>
        File.Exists(path) && new FileInfo(path).Length == file.Bytes
        && string.Equals(await _host.ComputeSha256Async(path, cancellationToken).ConfigureAwait(false), file.Sha256, StringComparison.OrdinalIgnoreCase);

    private Task<SetupFailure> DownloadAsync(SetupComponent component, string archive, CancellationToken cancellationToken)
    {
        _current = component;
        return DownloadAsync(component, component.Url, archive, this, mirror: true, cancellationToken);
    }

    private async Task<SetupFailure> DownloadAsync(SetupComponent component, Uri source, string archive, IProgress<double> progress, bool mirror, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt <= SetupRules.AutomaticRetries; attempt++)
        {
            if (attempt > 0)
            {
                Raise(new SetupEvent(SetupEventKind.Retrying, StateOf(component), attempt));
                WriteLog(AgentLogLevel.Warning, "Kurulum: indirme yeniden deneniyor: {Component} ({Attempt}/{Max})", component.Name, attempt, SetupRules.AutomaticRetries);
            }

            var result = await TryDownloadAsync(source, archive, progress, cancellationToken).ConfigureAwait(false);
            if (result is SetupFailure.None or SetupFailure.DiskFull)
            {
                return result;
            }
        }

        if (mirror && component.Mirrored && _mirrorBase is not null)
        {
            Raise(new SetupEvent(SetupEventKind.UsingMirror, StateOf(component)));
            WriteLog(AgentLogLevel.Warning, "Kurulum: resmi kaynağa ulaşılamadı, Pi aynasından indiriliyor: {Component}", component.Name);
            var result = await TryDownloadAsync(new Uri(_mirrorBase, component.FileName), archive, progress, cancellationToken).ConfigureAwait(false);
            if (result is SetupFailure.None or SetupFailure.DiskFull)
            {
                return result;
            }
        }

        return _host.IsOnline() ? SetupFailure.DownloadFailed : SetupFailure.NoInternet;
    }

    private async Task<SetupFailure> TryDownloadAsync(Uri source, string archive, IProgress<double> progress, CancellationToken cancellationToken)
    {
        try
        {
            await _host.DownloadAsync(source, archive, progress, cancellationToken).ConfigureAwait(false);
            return SetupFailure.None;
        }
        catch (IOException exception) when (IsDiskFull(exception))
        {
            return SetupFailure.DiskFull;
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            DeleteFile(archive);
            WriteLog(AgentLogLevel.Warning, "Kurulum: indirme başarısız: {Host} {ExceptionType}", source.Host, exception.GetType().Name);
            return SetupFailure.DownloadFailed;
        }
    }

    private async Task ReadyCheckAsync(CancellationToken cancellationToken)
    {
        Raise(new SetupEvent(SetupEventKind.Checking));
        var allReady = true;
        foreach (var component in _catalog)
        {
            // Sürümü listedekinden farklı kayıt (eskimiş isteğe bağlı model) denetlenmez: istenince yeniden kurulur (r2 Low-1).
            if (!_installed.TryGetValue(component.Id, out var record) || !string.Equals(record.Version, component.Version, StringComparison.Ordinal))
            {
                continue;
            }

            var ok = await IsIntactAsync(component, record, cancellationToken).ConfigureAwait(false);
            if (ok && component.CheckArguments is not null)
            {
                var output = await _host.RunAsync(ExecutablePath(component), component.CheckArguments, cancellationToken).ConfigureAwait(false);
                ok = output is not null && (component.CheckExpect is null || output.Contains(component.CheckExpect, StringComparison.Ordinal));
            }

            if (!ok)
            {
                allReady = false;
                Fail(component, SetupFailure.ReadyCheckFailed, InstallStep.Install, "Hazır denetiminden geçmedi (Ajan kurulumu eksik)");
            }
        }

        if (allReady)
        {
            IsReady = true;
            UpdatePercent();
            WriteLog(AgentLogLevel.Information, "Kurulum tamamlandı · Hazır");
            Raise(new SetupEvent(SetupEventKind.Ready));
        }
        else
        {
            Raise(new SetupEvent(SetupEventKind.Stopped));
        }
    }

    private async Task<bool> IsIntactAsync(SetupComponent component, InstalledComponent record, CancellationToken cancellationToken)
    {
        var executable = ExecutablePath(component);
        if (!File.Exists(executable))
        {
            return false;
        }

        // Çok dosyalı bileşen: büyük dosyalar açılışta hash'lenmez; varlık + boyut, dosya listesinin hash'i aşağıda (spec 0028 Req 3).
        foreach (var file in component.Files ?? [])
        {
            var path = Path.Combine(ComponentsDirectory, component.Id, file.Path.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path) || new FileInfo(path).Length != file.Bytes)
            {
                return false;
            }
        }

        var hash = await _host.ComputeSha256Async(executable, cancellationToken).ConfigureAwait(false);
        return string.Equals(hash, record.ExecutableSha256, StringComparison.OrdinalIgnoreCase);
    }

    private string ExecutablePath(SetupComponent component) =>
        Path.Combine(ComponentsDirectory, component.Id, component.Executable);

    private void CleanPartials()
    {
        try
        {
            if (Directory.Exists(ComponentsDirectory))
            {
                foreach (var partial in Directory.GetDirectories(ComponentsDirectory, "*.partial"))
                {
                    DeleteDirectory(partial);
                }
            }

            DeleteDirectory(DownloadsDirectory);
        }
        catch (IOException exception)
        {
            WriteLog(AgentLogLevel.Warning, "Kurulum: yarım dosyalar silinemedi: {ExceptionType}", exception.GetType().Name);
        }
    }

    private void Cleanup(string partial, string archive)
    {
        try
        {
            DeleteDirectory(partial);
            DeleteFile(archive);
        }
        catch (IOException)
        {
            // Bir sonraki açılışta CleanPartials yeniden dener.
        }
    }

    private void Fail(SetupComponent component, SetupFailure failure, InstallStep step, string reason)
    {
        _installed.Remove(component.Id);
        Persist();
        _currentFraction = 0;
        SetState(new ComponentState(component, ComponentStatus.Failed, failure, step), raise: true);
        WriteLog(AgentLogLevel.Error, "Kurulum hatası: {Component} {Reason} ({Failure}, adım {Step})", component.Name, reason, failure, step);
    }

    private void Persist()
    {
        try
        {
            _store.Save(new SetupState(new Dictionary<string, InstalledComponent>(_installed, StringComparer.Ordinal), _videoFolder));
        }
        catch (IOException exception)
        {
            WriteLog(AgentLogLevel.Warning, "Kurulum durumu yazılamadı: {ExceptionType}", exception.GetType().Name);
        }
    }

    private void SetStatus(SetupComponent component, ComponentStatus status) =>
        SetState(new ComponentState(component, status), raise: true);

    private void SetState(ComponentState state, bool raise)
    {
        lock (_gate)
        {
            var index = Array.FindIndex(_states, s => s.Component.Id == state.Component.Id);
            _states[index] = state;
        }

        UpdatePercent();
        if (raise)
        {
            Raise(new SetupEvent(SetupEventKind.StatusChanged, state));
        }
    }

    private ComponentState StateOf(SetupComponent component) =>
        Components.First(s => s.Component.Id == component.Id);

    /// <inheritdoc />
    void IProgress<double>.Report(double value)
    {
        if (_current is { } component)
        {
            ReportProgress(component, _spanStart + (value * _spanWidth));
        }
    }

    private void ReportProgress(SetupComponent component, double fraction)
    {
        _currentFraction = Math.Clamp(fraction, 0, 1);
        var before = Percent;
        UpdatePercent();
        if (Percent != before)
        {
            Raise(new SetupEvent(SetupEventKind.Progress, StateOf(component)));
        }
    }

    private void UpdatePercent()
    {
        var states = Components;
        if (states.Count == 0)
        {
            Volatile.Write(ref _percent, 100);
            return;
        }

        var done = states.Count(s => s.Status is ComponentStatus.Installed or ComponentStatus.Skipped);
        var value = (int)Math.Floor((done + _currentFraction) * 100 / states.Count);
        Volatile.Write(ref _percent, Math.Clamp(value, 0, 100));
    }

    private void Raise(SetupEvent setupEvent) => Changed?.Invoke(this, setupEvent);
}
