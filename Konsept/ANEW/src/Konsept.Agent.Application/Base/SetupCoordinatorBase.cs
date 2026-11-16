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
    private int _percent;

    /// <summary>Initializes a new instance of the <see cref="SetupCoordinatorBase"/> class.</summary>
    /// <param name="catalog">Bileşen listesi (kurulum sırası).</param>
    /// <param name="host">Dış dünya.</param>
    /// <param name="store">Kalıcı durum.</param>
    /// <param name="installRoot">Kurulum yeri.</param>
    /// <param name="defaultVideoFolder">Varsayılan video klasörü.</param>
    /// <param name="mirrorBase">Pi aynasının adresi (<c>…/ajan-bilesenleri/</c>); yoksa <c>null</c>.</param>
    protected SetupCoordinatorBase(
        IReadOnlyList<SetupComponent> catalog,
        ISetupHost host,
        ISetupStateStore store,
        string installRoot,
        string defaultVideoFolder,
        Uri? mirrorBase)
    {
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
            var status = ComponentStatus.Waiting;
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

        IsReady = _states.All(s => s.Status == ComponentStatus.Installed);
        UpdatePercent();
        return !IsReady;
    }

    /// <inheritdoc />
    public SetupPreflight Preflight()
    {
        var pending = Components.Where(s => s.Status != ComponentStatus.Installed).Select(s => s.Component);
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
    public Task RunAsync(CancellationToken cancellationToken) => RunCoreAsync(null, cancellationToken);

    /// <inheritdoc />
    public Task RetryAsync(string componentId, CancellationToken cancellationToken) => RunCoreAsync(componentId, cancellationToken);

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

    private async Task RunCoreAsync(string? onlyId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_running)
            {
                return;
            }

            _running = true;
        }

        IsReady = false;
        try
        {
            var targets = Components
                .Where(s => onlyId is null ? s.Status is not ComponentStatus.Installed and not ComponentStatus.Skipped : s.Component.Id == onlyId)
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
    }

    private async Task InstallAsync(SetupComponent component, CancellationToken cancellationToken)
    {
        var archive = Path.Combine(DownloadsDirectory, component.FileName);
        var partial = Path.Combine(ComponentsDirectory, component.Id + ".partial");
        var step = InstallStep.Download;
        try
        {
            _currentFraction = 0;
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

    private async Task<SetupFailure> DownloadAsync(SetupComponent component, string archive, CancellationToken cancellationToken)
    {
        _current = component;
        IProgress<double> progress = this;
        for (var attempt = 0; attempt <= SetupRules.AutomaticRetries; attempt++)
        {
            if (attempt > 0)
            {
                Raise(new SetupEvent(SetupEventKind.Retrying, StateOf(component), attempt));
                WriteLog(AgentLogLevel.Warning, "Kurulum: indirme yeniden deneniyor: {Component} ({Attempt}/{Max})", component.Name, attempt, SetupRules.AutomaticRetries);
            }

            var result = await TryDownloadAsync(component.Url, archive, progress, cancellationToken).ConfigureAwait(false);
            if (result is SetupFailure.None or SetupFailure.DiskFull)
            {
                return result;
            }
        }

        if (component.Mirrored && _mirrorBase is not null)
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
            if (!_installed.TryGetValue(component.Id, out var record))
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
            ReportProgress(component, value);
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
