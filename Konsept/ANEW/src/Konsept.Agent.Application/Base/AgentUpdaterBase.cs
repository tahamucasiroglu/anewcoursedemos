using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Domain.Rules;

namespace Konsept.Agent.Application.Base;

/// <summary>
/// <see cref="IAgentUpdater"/> mantığı (spec 0023). Liste ve imzası (<c>manifest.json</c>, <c>manifest.json.sig</c>) Pi'deki güncelleme
/// adresinden indirilir; imza geçmezse hiçbir şey kurulmaz (Error), eski/aynı sürüm reddedilir (Warning, "Ajan güncel"). Güncelleme yalnızca
/// <see cref="ApplyAsync"/> ile (kullanıcı onayı) başlar; sunucu "Üretiliyor" iş bildirirse 15 sn arayla yeniden sorar ve iş bitince
/// kendiliğinden başlar. Exe 2 otomatik yeniden denemeyle indirilir, SHA-256'sı listeyle karşılaştırılır, tutmazsa silinir. Dış dünya
/// (<see cref="ISetupHost"/>, <see cref="IAgentInstallation"/>, <see cref="IServerConnection"/>) soyutlamaların arkasındadır; log alt sınıfta.
/// </summary>
public abstract class AgentUpdaterBase : IAgentUpdater, IProgress<double>
{
    /// <summary>Yeni sürüm ilk açılışta bu argümanla başlatılır.</summary>
    public const string AfterUpdateArgument = "--guncelleme-sonrasi";

    private readonly ISetupHost _host;
    private readonly IManifestVerifier _verifier;
    private readonly IAgentInstallation _installation;
    private readonly IServerConnection _connection;
    private readonly Uri _feed;
    private readonly TimeProvider _time;
    private readonly SemaphoreSlim _applyGate = new(1, 1);
    private readonly SemaphoreSlim _checkGate = new(1, 1);
    private UpdateEvent _current = new(UpdateStatus.Idle);

    /// <summary>Initializes a new instance of the <see cref="AgentUpdaterBase"/> class.</summary>
    /// <param name="currentVersion">Kurulu sürüm.</param>
    /// <param name="feed">Güncelleme adresi (<c>{site}/ajan-guncelleme/</c>).</param>
    /// <param name="host">İndirme, SHA-256, disk.</param>
    /// <param name="verifier">İmza doğrulayıcı.</param>
    /// <param name="installation">Kurulu kopya.</param>
    /// <param name="connection">Sunucu bağlantısı ("Üretiliyor" denetimi, ilk açılış onayı).</param>
    /// <param name="time">Saat.</param>
    protected AgentUpdaterBase(
        string currentVersion,
        Uri feed,
        ISetupHost host,
        IManifestVerifier verifier,
        IAgentInstallation installation,
        IServerConnection connection,
        TimeProvider time)
    {
        CurrentVersion = currentVersion ?? throw new ArgumentNullException(nameof(currentVersion));
        _feed = feed ?? throw new ArgumentNullException(nameof(feed));
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _verifier = verifier ?? throw new ArgumentNullException(nameof(verifier));
        _installation = installation ?? throw new ArgumentNullException(nameof(installation));
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _time = time ?? throw new ArgumentNullException(nameof(time));
    }

    /// <inheritdoc />
    public event EventHandler<UpdateEvent>? Changed;

    /// <inheritdoc />
    public event EventHandler<UpdateManifest>? AvailableFound;

    /// <inheritdoc />
    public event EventHandler? RestartRequested;

    /// <inheritdoc />
    public string CurrentVersion { get; }

    /// <inheritdoc />
    public UpdateManifest? Available { get; private set; }

    /// <inheritdoc />
    public UpdateEvent Current => Volatile.Read(ref _current);

    /// <inheritdoc />
    public async Task<UpdateCheckOutcome> CheckAsync(CancellationToken cancellationToken)
    {
        if (!_verifier.IsConfigured)
        {
            return UpdateCheckOutcome.Disabled;
        }

        // Açılış, zamanlayıcı, tray ve sürüm uyumsuzluğu aynı anda denetleyebilir; aynı geçici dosyaları paylaşırlar (review r1 M-2).
        await _checkGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await CheckCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _checkGate.Release();
        }
    }

    private async Task<UpdateCheckOutcome> CheckCoreAsync(CancellationToken cancellationToken)
    {

        WriteLog(AgentLogLevel.Information, "Güncelleme denetimi: kurulu sürüm {Version}", CurrentVersion);
        byte[] manifestBytes;
        byte[] signature;
        try
        {
            Directory.CreateDirectory(_installation.StagingDirectory);
            manifestBytes = await FetchAsync("manifest.json", cancellationToken).ConfigureAwait(false);
            signature = await FetchAsync("manifest.json.sig", cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            WriteLog(AgentLogLevel.Warning, "Güncelleme denetimi: sunucuya ulaşılamadı ({ExceptionType})", ex.GetType().Name);
            return UpdateCheckOutcome.Unavailable;
        }

        var manifest = _verifier.Verify(manifestBytes, signature) ? UpdateRules.Parse(manifestBytes) : null;
        if (manifest is null)
        {
            WriteLog(AgentLogLevel.Error, "Güncelleme doğrulanamadı: sürüm listesinin imzası ya da biçimi geçersiz; mevcut sürümle devam ediliyor");
            Available = null;
            Raise(new UpdateEvent(UpdateStatus.VerificationFailed));
            return UpdateCheckOutcome.VerificationFailed;
        }

        if (!UpdateRules.IsNewer(manifest.Version, CurrentVersion))
        {
            WriteLog(AgentLogLevel.Warning, "Eski ya da aynı sürümü taşıyan liste reddedildi: {Offered} (kurulu {Version})", manifest.Version, CurrentVersion);
            Available = null;
            return UpdateCheckOutcome.UpToDate;
        }

        WriteLog(AgentLogLevel.Information, "Yeni sürüm bulundu: {Offered} (kurulu {Version})", manifest.Version, CurrentVersion);
        Available = manifest;
        if (Current.Status is UpdateStatus.Idle or UpdateStatus.VerificationFailed or UpdateStatus.Available)
        {
            Raise(new UpdateEvent(UpdateStatus.Available, Version: manifest.Version));
        }

        return UpdateCheckOutcome.Available;
    }

    /// <inheritdoc />
    public async Task RunScheduleAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (await CheckAsync(cancellationToken).ConfigureAwait(false) == UpdateCheckOutcome.Available && Available is { } manifest)
                {
                    AvailableFound?.Invoke(this, manifest); // Yalnızca haber verir; onaysız güncelleme yok (Req 2).
                }

                await Task.Delay(UpdateRules.CheckInterval, _time, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    /// <inheritdoc />
    public async Task<bool> ApplyAsync(CancellationToken cancellationToken)
    {
        if (Available is not { } manifest || !await _applyGate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        try
        {
            // "Üretiliyor" iş varken başlamaz (Req 3); iş bitince ya da durdurulunca kendiliğinden başlar. Sunucuya ulaşılamıyorsa ajana zaten iş gitmez.
            while (await _connection.TryBeginUpdateAsync(cancellationToken).ConfigureAwait(false) == false)
            {
                if (Current.Status != UpdateStatus.WaitingForProduction)
                {
                    WriteLog(AgentLogLevel.Information, "Güncelleme bekliyor: üretim sürüyor");
                    Raise(new UpdateEvent(UpdateStatus.WaitingForProduction, Version: manifest.Version));
                }

                await Task.Delay(UpdateRules.ProductionRetryInterval, _time, cancellationToken).ConfigureAwait(false);
            }

            WriteLog(AgentLogLevel.Information, "Güncelleme başladı: {Offered} (kurulu {Version})", manifest.Version, CurrentVersion);
            var installed = false;
            try
            {
                installed = await InstallAsync(manifest, cancellationToken).ConfigureAwait(false);
                return installed;
            }
            finally
            {
                if (!installed)
                {
                    // Kurulmadan bitti: sunucu ajana yeniden iş göndersin (review r1 H-1; AC-4 "API'ye bağlı kalır").
                    await _connection.TryEndUpdateAsync().ConfigureAwait(false);
                }
            }
        }
        finally
        {
            _applyGate.Release();
        }
    }

    /// <inheritdoc />
    public Task<UpdateStartup> StartupAsync(CancellationToken cancellationToken)
    {
        CleanStaging();
        var state = _installation.LoadState();
        if (state.RolledBackFrom is { } failed)
        {
            // Önceki sürüm açıldı: bildirim bir kez (Req 9).
            WriteLog(AgentLogLevel.Warning, "Önceki sürümle açıldı; başlatılamayan sürüm: {Failed}", failed);
            _installation.SaveState(state with { RolledBackFrom = null });
            return Task.FromResult(UpdateStartup.RolledBack);
        }

        if (!string.Equals(state.PendingVersion, CurrentVersion, StringComparison.Ordinal))
        {
            return Task.FromResult(UpdateStartup.Normal);
        }

        if (state.PendingLaunches >= 1)
        {
            // Önceki açılış onaylanmadan kesildi (çökme, kapanma): yeni sürüm yarıda kaldı → önceki sürüm (Req 9, AC-7).
            return Task.FromResult(RollBack(state, "Yeni sürüm önceki açılışta onaylanmadan kesildi") ? UpdateStartup.Restarting : UpdateStartup.Normal);
        }

        _installation.SaveState(state with { PendingLaunches = 1 });
        _ = WatchFirstLaunchAsync(state, cancellationToken);
        return Task.FromResult(UpdateStartup.Watching);
    }

    /// <summary>İndirme ilerlemesi (eşzamanlı; olay sırası korunur).</summary>
    /// <param name="value">0–1.</param>
    void IProgress<double>.Report(double value) =>
        Raise(new UpdateEvent(UpdateStatus.Downloading, (int)Math.Round(Math.Clamp(value, 0, 1) * 100), Version: Available?.Version));

    /// <summary>Log yazar (Serilog alt sınıfta; kurulum hataları 0022 uyarısını kendiliğinden açmaz — satırdaki "Hatayı bildir" açar).</summary>
    /// <param name="level">Seviye.</param>
    /// <param name="messageTemplate">Şablon (sır içermez).</param>
    /// <param name="values">Değerler.</param>
    protected abstract void WriteLog(AgentLogLevel level, string messageTemplate, params object?[] values);

    private async Task<bool> InstallAsync(UpdateManifest manifest, CancellationToken cancellationToken)
    {
        var staged = Path.Combine(_installation.StagingDirectory, manifest.FileName);
        Raise(new UpdateEvent(UpdateStatus.Downloading, Version: manifest.Version));
        if (_host.FreeBytes(_installation.InstallRoot) < UpdateRules.RequiredBytes(manifest))
        {
            return Fail(manifest, SetupFailure.DiskFull, InstallStep.Download);
        }

        if (!await DownloadAsync(manifest, staged, cancellationToken).ConfigureAwait(false))
        {
            return Fail(manifest, _host.IsOnline() ? SetupFailure.DownloadFailed : SetupFailure.NoInternet, InstallStep.Download);
        }

        Raise(new UpdateEvent(UpdateStatus.Verifying, 100, Version: manifest.Version));
        var sha = await _host.ComputeSha256Async(staged, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(sha, manifest.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            TryDelete(staged);
            WriteLog(AgentLogLevel.Error, "Güncelleme doğrulanamadı: {File} SHA-256 listeyle tutmuyor; kurulmadı, mevcut sürümle devam ediliyor", manifest.FileName);
            Raise(new UpdateEvent(UpdateStatus.VerificationFailed, Version: manifest.Version));
            return false;
        }

        Raise(new UpdateEvent(UpdateStatus.Installing, 100, Version: manifest.Version));
        var before = _installation.LoadState();
        try
        {
            // Önce durum, sonra değiş-tokuş (review r1 L-1): arada kesilirse yeni exe "onay bekliyor" olarak açılır ve izlenir.
            _installation.SaveState(new UpdateState(manifest.Version, 0, CurrentVersion));
            _installation.Swap(staged);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            WriteLog(AgentLogLevel.Error, "Güncelleme kurulamadı: {ExceptionType}", ex.GetType().Name);
            try
            {
                _installation.SaveState(before);
            }
            catch (Exception restore) when (restore is IOException or UnauthorizedAccessException)
            {
                // Durum dosyası yazılamıyor: ekran yine Hata + Tekrar dene gösterir (review r2 L-1).
                WriteLog(AgentLogLevel.Warning, "Güncelleme durumu geri yazılamadı: {ExceptionType}", restore.GetType().Name);
            }

            return Fail(manifest, SetupFailure.InstallFailed, InstallStep.Install);
        }

        WriteLog(AgentLogLevel.Information, "Bileşen güncellendi: ajan {Offered}", manifest.Version);
        Raise(new UpdateEvent(UpdateStatus.Completed, 100, Version: manifest.Version));
        _installation.LaunchInstalled(AfterUpdateArgument);
        RestartRequested?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private async Task<bool> DownloadAsync(UpdateManifest manifest, string staged, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt <= SetupRules.AutomaticRetries; attempt++)
        {
            try
            {
                await _host.DownloadAsync(new Uri(_feed, manifest.FileName), staged, this, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                TryDelete(staged);
                WriteLog(AgentLogLevel.Warning, "Güncelleme indirilemedi ({Attempt}/{Max}): {ExceptionType}", attempt + 1, SetupRules.AutomaticRetries + 1, ex.GetType().Name);
            }
        }

        return false;
    }

    private bool Fail(UpdateManifest manifest, SetupFailure failure, InstallStep step)
    {
        WriteLog(AgentLogLevel.Error, "Güncelleme hatası: ajan {Offered} ({Failure}, adım {Step})", manifest.Version, failure, step);
        Raise(new UpdateEvent(UpdateStatus.Failed, Failure: failure, Version: manifest.Version));
        return false;
    }

    private async Task WatchFirstLaunchAsync(UpdateState state, CancellationToken cancellationToken)
    {
        var connected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnState(object? sender, ConnectionState s)
        {
            if (s == ConnectionState.Connected)
            {
                connected.TrySetResult();
            }
        }

        _connection.StateChanged += OnState;
        try
        {
            if (_connection.State == ConnectionState.Connected)
            {
                connected.TrySetResult();
            }

            try
            {
                await connected.Task.WaitAsync(UpdateRules.CommitTimeout, _time, cancellationToken).ConfigureAwait(false);
                _installation.SaveState(new UpdateState(PreviousVersion: state.PreviousVersion));
                WriteLog(AgentLogLevel.Information, "Güncelleme onaylandı: {Version} API'ye bağlandı", CurrentVersion);
            }
            catch (TimeoutException)
            {
                if (RollBack(_installation.LoadState(), "Yeni sürüm 60 sn içinde API'ye bağlanamadı"))
                {
                    RestartRequested?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Uygulama kapanıyor: onaysız kalır, sonraki açılış geri döner.
        }
        finally
        {
            _connection.StateChanged -= OnState;
        }
    }

    private bool RollBack(UpdateState state, string reason)
    {
        WriteLog(AgentLogLevel.Error, "Geri dönüş: {Reason}; {Version} → önceki sürüm {Previous}", reason, CurrentVersion, state.PreviousVersion);
        if (!_installation.RollBack())
        {
            WriteLog(AgentLogLevel.Error, "Geri dönüş yapılamadı: önceki sürüm bulunamadı");
            _installation.SaveState(new UpdateState());
            return false;
        }

        _installation.SaveState(new UpdateState(RolledBackFrom: CurrentVersion));
        _installation.LaunchInstalled(string.Empty);
        return true;
    }

    private async Task<byte[]> FetchAsync(string name, CancellationToken cancellationToken)
    {
        var target = Path.Combine(_installation.StagingDirectory, name);
        try
        {
            await _host.DownloadAsync(new Uri(_feed, name), target, null, cancellationToken).ConfigureAwait(false);
            return await File.ReadAllBytesAsync(target, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            TryDelete(target);
        }
    }

    private void CleanStaging()
    {
        try
        {
            if (Directory.Exists(_installation.StagingDirectory))
            {
                Directory.Delete(_installation.StagingDirectory, recursive: true); // Yarım kalan indirme kurulmaz (AC-7).
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            WriteLog(AgentLogLevel.Warning, "Yarım güncelleme dosyaları silinemedi: {ExceptionType}", ex.GetType().Name);
        }
    }

    private void Raise(UpdateEvent update)
    {
        Volatile.Write(ref _current, update);
        Changed?.Invoke(this, update);
    }

    private static void TryDelete(string file)
    {
        try
        {
            File.Delete(file);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Bir sonraki açılışta staging temizlenir.
        }
    }
}
