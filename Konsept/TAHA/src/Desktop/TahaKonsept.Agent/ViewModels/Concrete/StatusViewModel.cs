using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Account.Abstract;
using TahaKonsept.Agent.Core.Auth.Abstract;
using TahaKonsept.Agent.Core.Capabilities.Abstract;
using TahaKonsept.Agent.Core.Execution;
using TahaKonsept.Agent.Core.Execution.Abstract;
using TahaKonsept.Agent.Core.Hub;
using TahaKonsept.Agent.Core.Hub.Abstract;
using TahaKonsept.Agent.Core.Pairing.Abstract;
using TahaKonsept.Agent.EngineList.Abstract;
using TahaKonsept.Agent.Shell.Abstract;
using TahaKonsept.Agent.Startup.Abstract;
using TahaKonsept.Agent.Threading.Abstract;
using TahaKonsept.Agent.ViewModels.Base;
using TahaKonsept.Agent.WebSite.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Agent.ViewModels.Concrete;

/// <summary>
/// Durum penceresi: sunucu bağlantısı (<see cref="HubStatusText"/>), eşlenen hesap (<see cref="AccountName"/>), motor listesi
/// (<see cref="Engines"/>, "Kur" düğmeli), GPU özeti, çalışan iş ilerlemesi (<see cref="CurrentJobText"/>/<see cref="CurrentJobPercent"/>),
/// "Windows ile başlat" (<see cref="StartWithWindows"/>), log/video klasörünü açma, siteye gitme, eşlemeyi kaldırma ve yeniden eşleme
/// (<see cref="PairRequested"/> → kurulum penceresi).
/// </summary>
public sealed class StatusViewModel : ViewModelBase
{
    private readonly IAgentHubConnection _hub;
    private readonly IDevicePairingService _pairing;
    private readonly IAgentAuthService _auth;
    private readonly IPairedAccountReader _account;
    private readonly IEngineCatalog _engineCatalog;
    private readonly ICapabilityDetector _capabilities;
    private readonly IJobRunner _jobs;
    private readonly IAgentPaths _paths;
    private readonly IDesktopLauncher _launcher;
    private readonly IStartupRegistration _startup;
    private readonly IWebSiteLocator _webSite;
    private readonly IUiDispatcher _dispatcher;
    private readonly ILogger<StatusViewModel> _logger;
    private HubConnectionStatus _hubStatus;
    private string? _accountName;
    private bool _isPaired;
    private string _gpuSummary = "Ekran kartı algılanıyor…";
    private string? _currentJobText;
    private double _currentJobPercent;
    private bool _startWithWindows;
    private string? _notice;

    /// <summary>Oluşturur.</summary>
    /// <param name="hub">Hub bağlantısı.</param>
    /// <param name="pairing">Eşleme servisi.</param>
    /// <param name="auth">Oturum.</param>
    /// <param name="account">Hesap adı okuyucu.</param>
    /// <param name="engineCatalog">Motor listesi.</param>
    /// <param name="capabilities">Donanım algılayıcı.</param>
    /// <param name="jobs">İş çalıştırıcı.</param>
    /// <param name="paths">Ajan klasörleri.</param>
    /// <param name="launcher">Tarayıcı/Gezgin açıcı.</param>
    /// <param name="startup">Windows ile başlat kaydı.</param>
    /// <param name="webSite">Web adresi.</param>
    /// <param name="dispatcher">UI gönderici.</param>
    /// <param name="logger">Log.</param>
    public StatusViewModel(
        IAgentHubConnection hub,
        IDevicePairingService pairing,
        IAgentAuthService auth,
        IPairedAccountReader account,
        IEngineCatalog engineCatalog,
        ICapabilityDetector capabilities,
        IJobRunner jobs,
        IAgentPaths paths,
        IDesktopLauncher launcher,
        IStartupRegistration startup,
        IWebSiteLocator webSite,
        IUiDispatcher dispatcher,
        ILogger<StatusViewModel> logger)
    {
        _hub = hub;
        _pairing = pairing;
        _auth = auth;
        _account = account;
        _engineCatalog = engineCatalog;
        _capabilities = capabilities;
        _jobs = jobs;
        _paths = paths;
        _launcher = launcher;
        _startup = startup;
        _webSite = webSite;
        _dispatcher = dispatcher;
        _logger = logger;

        OpenLogsCommand = new RelayCommand(() => ShowResult(_launcher.OpenFolder(_paths.LogsDir)));
        OpenVideosCommand = new RelayCommand(() => ShowResult(_launcher.OpenFolder(_paths.VideosDir)));
        OpenWebCommand = new RelayCommand(() => ShowResult(_launcher.OpenUrl(_webSite.GetWebUrl())));
        UnpairCommand = new RelayCommand(Unpair, () => IsPaired);
        PairCommand = new RelayCommand(() => PairRequested?.Invoke(this, EventArgs.Empty), () => !IsPaired);

        foreach (var descriptor in _engineCatalog.GetEngines())
        {
            Engines.Add(new EngineItemViewModel(descriptor, dispatcher, logger));
        }

        _startWithWindows = _startup.IsEnabled;
        _hubStatus = _hub.Status;
        RefreshAccount();
        UpdateJob(_jobs.GetActiveStages().FirstOrDefault());

        _hub.StatusChanged += OnHubStatusChanged;
        _auth.AuthorizationChanged += OnAuthorizationChanged;
        _jobs.StageUpdated += OnStageUpdated;
    }

    /// <summary>Kullanıcı "Eşle" deyince tetiklenir (kurulum penceresinin eşleme adımı açılır).</summary>
    public event EventHandler? PairRequested;

    /// <summary>Motorlar.</summary>
    public ObservableCollection<EngineItemViewModel> Engines { get; } = [];

    /// <summary>Log klasörünü aç.</summary>
    public IRelayCommand OpenLogsCommand { get; }

    /// <summary>Videolar klasörünü aç.</summary>
    public IRelayCommand OpenVideosCommand { get; }

    /// <summary>Siteye git.</summary>
    public IRelayCommand OpenWebCommand { get; }

    /// <summary>Eşlemeyi kaldır.</summary>
    public IRelayCommand UnpairCommand { get; }

    /// <summary>Eşle (kurulum penceresinde).</summary>
    public IRelayCommand PairCommand { get; }

    /// <summary>Hub durumu.</summary>
    public HubConnectionStatus HubStatus
    {
        get => _hubStatus;
        private set
        {
            if (SetProperty(ref _hubStatus, value))
            {
                OnPropertyChanged(nameof(HubStatusText));
                OnPropertyChanged(nameof(IsConnected));
            }
        }
    }

    /// <summary>Bağlı mı.</summary>
    public bool IsConnected => HubStatus == HubConnectionStatus.Connected;

    /// <summary>Hub durumu (Türkçe).</summary>
    public string HubStatusText => DescribeHubStatus(HubStatus);

    /// <summary>Eşlenen hesap.</summary>
    public string? AccountName
    {
        get => _accountName;
        private set
        {
            if (SetProperty(ref _accountName, value))
            {
                OnPropertyChanged(nameof(AccountText));
            }
        }
    }

    /// <summary>Hesap satırı metni.</summary>
    public string AccountText => !IsPaired ? "Eşlenmedi" : string.IsNullOrWhiteSpace(AccountName) ? "Eşlendi" : AccountName!;

    /// <summary>Eşli mi.</summary>
    public bool IsPaired
    {
        get => _isPaired;
        private set
        {
            if (SetProperty(ref _isPaired, value))
            {
                OnPropertyChanged(nameof(AccountText));
                UnpairCommand.NotifyCanExecuteChanged();
                PairCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>GPU/VRAM özeti.</summary>
    public string GpuSummary
    {
        get => _gpuSummary;
        private set => SetProperty(ref _gpuSummary, value);
    }

    /// <summary>Çalışan iş metni (yoksa <c>null</c>).</summary>
    public string? CurrentJobText
    {
        get => _currentJobText;
        private set
        {
            if (SetProperty(ref _currentJobText, value))
            {
                OnPropertyChanged(nameof(HasActiveJob));
                OnPropertyChanged(nameof(JobSummaryText));
            }
        }
    }

    /// <summary>Çalışan iş yüzdesi.</summary>
    public double CurrentJobPercent
    {
        get => _currentJobPercent;
        private set => SetProperty(ref _currentJobPercent, Math.Clamp(value, 0, 100));
    }

    /// <summary>Çalışan iş var mı.</summary>
    public bool HasActiveJob => CurrentJobText is not null;

    /// <summary>İş özeti (iş yoksa "Şu an iş yok").</summary>
    public string JobSummaryText => CurrentJobText ?? "Şu an çalışan iş yok.";

    /// <summary>Windows ile başlat.</summary>
    public bool StartWithWindows
    {
        get => _startWithWindows;
        set
        {
            if (_startWithWindows == value)
            {
                return;
            }

            var result = _startup.SetEnabled(value);
            if (result.IsFailure)
            {
                Notice = result.Error.Message;
                OnPropertyChanged();
                return;
            }

            SetProperty(ref _startWithWindows, value);
            _logger.LogInformation("Windows ile başlat: {Enabled}", value);
        }
    }

    /// <summary>Kısa bilgi/hata satırı.</summary>
    public string? Notice
    {
        get => _notice;
        private set => SetProperty(ref _notice, value);
    }

    /// <summary>Ajan sürümü.</summary>
    public string VersionText { get; } = "Sürüm " + (typeof(StatusViewModel).Assembly.GetName().Version?.ToString(3) ?? "0.0.0");

    /// <summary>Hub durumunu Türkçe metne çevirir.</summary>
    /// <param name="status">Durum.</param>
    /// <returns>Metin.</returns>
    public static string DescribeHubStatus(HubConnectionStatus status) => status switch
    {
        HubConnectionStatus.Connected => "Sunucuya bağlı",
        HubConnectionStatus.Connecting => "Bağlanıyor…",
        HubConnectionStatus.Reconnecting => "Bağlantı koptu, yeniden deneniyor…",
        HubConnectionStatus.Unauthorized => "Eşleme bekleniyor (yetki yok)",
        _ => "Bağlı değil",
    };

    /// <summary>Çalışan aşamayı Türkçe metne çevirir.</summary>
    /// <param name="info">Aşama bilgisi.</param>
    /// <returns>Metin.</returns>
    public static string DescribeStage(RunningStageInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);
        var stage = info.Stage switch
        {
            PipelineStage.Images => "Görseller",
            PipelineStage.Speech => "Seslendirme",
            PipelineStage.Align => "Altyazı hizalama",
            PipelineStage.SceneRender => "Sahne render",
            PipelineStage.Compose => "Birleştirme",
            PipelineStage.Mux => "Son video",
            _ => info.Stage.ToString(),
        };
        var scene = info.SceneIndex is { } i ? " (sahne " + (i + 1).ToString(CultureInfo.InvariantCulture) + ")" : string.Empty;
        var percent = info.Percent.ToString("0", CultureInfo.InvariantCulture);
        return stage + scene + " · %" + percent + (string.IsNullOrWhiteSpace(info.Message) ? string.Empty : " · " + info.Message);
    }

    /// <summary>Donanımı algılar ve motorların VRAM uyarısını günceller.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public async Task LoadAsync(CancellationToken ct)
    {
        try
        {
            var caps = await _capabilities.DetectAsync(ct).ConfigureAwait(false);
            var summary = SetupViewModel.DescribeGpus(caps.Gpus);
            long? vram = caps.Gpus.Count == 0 ? null : caps.Gpus.Max(g => g.VramMb);
            _dispatcher.Post(() =>
            {
                GpuSummary = summary;
                foreach (var engine in Engines)
                {
                    engine.SetGpuVram(vram);
                }
            });
        }
        catch (OperationCanceledException)
        {
            // Pencere kapandı.
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or System.ComponentModel.Win32Exception)
        {
            _logger.LogWarning(ex, "Donanım algılanamadı.");
            _dispatcher.Post(() => GpuSummary = "Ekran kartı algılanamadı.");
        }
    }

    /// <inheritdoc />
    protected override void OnDisposing()
    {
        _hub.StatusChanged -= OnHubStatusChanged;
        _auth.AuthorizationChanged -= OnAuthorizationChanged;
        _jobs.StageUpdated -= OnStageUpdated;
        foreach (var engine in Engines)
        {
            engine.Dispose();
        }
    }

    private void Unpair()
    {
        _pairing.Unpair();
        _logger.LogInformation("Eşleme kullanıcı tarafından kaldırıldı.");
        Notice = "Eşleme kaldırıldı. Yeniden eşlemek için \"Eşle\" düğmesini kullanın.";
        RefreshAccount();
    }

    private void RefreshAccount()
    {
        IsPaired = _auth.IsPaired;
        AccountName = IsPaired ? _account.GetAccountName() : null;
    }

    private void ShowResult(Result result) => Notice = result.IsFailure ? result.Error.Message : null;

    private void OnHubStatusChanged(object? sender, HubConnectionStatus status) => _dispatcher.Post(() =>
    {
        HubStatus = status;
        RefreshAccount();
    });

    private void OnAuthorizationChanged(object? sender, EventArgs e) => _dispatcher.Post(RefreshAccount);

    private void OnStageUpdated(object? sender, RunningStageInfo info) => _dispatcher.Post(() => UpdateJob(_jobs.GetActiveStages().FirstOrDefault()));

    private void UpdateJob(RunningStageInfo? info)
    {
        if (IsDisposed)
        {
            return;
        }

        if (info is null)
        {
            CurrentJobText = null;
            CurrentJobPercent = 0;
            return;
        }

        CurrentJobText = DescribeStage(info);
        CurrentJobPercent = info.Percent;
    }
}
