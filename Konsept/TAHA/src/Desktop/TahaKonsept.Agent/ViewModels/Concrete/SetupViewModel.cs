using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Core.Auth.Abstract;
using TahaKonsept.Agent.Core.Capabilities.Abstract;
using TahaKonsept.Agent.Core.Configuration;
using TahaKonsept.Agent.Core.Configuration.Abstract;
using TahaKonsept.Agent.Core.Prerequisites;
using TahaKonsept.Agent.Core.Prerequisites.Abstract;
using TahaKonsept.Agent.Settings.Abstract;
using TahaKonsept.Agent.Threading;
using TahaKonsept.Agent.Threading.Abstract;
using TahaKonsept.Agent.ViewModels.Base;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Agent.ViewModels.Concrete;

/// <summary>
/// İlk kurulum penceresi (durum makinesi, <see cref="Phase"/>):
/// <see cref="SetupPhase.Prerequisites"/> → (<see cref="InstallCommand"/>) <see cref="SetupPhase.Installing"/> →
/// başarılıysa <see cref="SetupPhase.Pairing"/> (zaten eşliyse doğrudan <see cref="SetupPhase.Completed"/>), zorunlu bileşen kurulamadıysa
/// <see cref="SetupPhase.Failed"/> (<see cref="InstallCommand"/> = Tekrar dene). Eşleme bitince <see cref="SetupPhase.Completed"/> ve ayar kaydı.
/// Adımlar <see cref="Steps"/> (ilerleme çubuklu), günlük <see cref="Logs"/>, GPU/VRAM özeti <see cref="GpuSummary"/>, sunucu adresi <see cref="ServerUrl"/>.
/// </summary>
public sealed class SetupViewModel : ViewModelBase
{
    /// <summary>Günlük panelinde tutulan en fazla satır.</summary>
    public const int MaxLogLines = 500;

    private readonly IPrerequisiteInstaller _installer;
    private readonly ICapabilityDetector _capabilities;
    private readonly IAgentSettingsStore _agentSettings;
    private readonly IDesktopAppSettingsStore _desktopSettings;
    private readonly IAgentAuthService _auth;
    private readonly AgentOptions _options;
    private readonly IUiDispatcher _dispatcher;
    private readonly ILogger<SetupViewModel> _logger;
    private CancellationTokenSource? _installCts;
    private SetupPhase _phase;
    private double _overallPercent;
    private string _statusMessage = "Gerekli bileşenler denetleniyor…";
    private string _gpuSummary = "Ekran kartı algılanıyor…";
    private string _serverUrl;
    private string? _serverUrlError;

    /// <summary>Oluşturur.</summary>
    /// <param name="installer">Ön gereksinim kurucusu.</param>
    /// <param name="capabilities">Donanım algılayıcı.</param>
    /// <param name="agentSettings">Çekirdek ayarları (sunucu adresi).</param>
    /// <param name="desktopSettings">Masaüstü ayarları (kurulum bitti işareti).</param>
    /// <param name="auth">Oturum (zaten eşli mi).</param>
    /// <param name="options">Çekirdek seçenekleri (varsayılan sunucu adresi).</param>
    /// <param name="pairing">Eşleme adımı görünüm modeli.</param>
    /// <param name="dispatcher">UI gönderici.</param>
    /// <param name="logger">Log.</param>
    public SetupViewModel(
        IPrerequisiteInstaller installer,
        ICapabilityDetector capabilities,
        IAgentSettingsStore agentSettings,
        IDesktopAppSettingsStore desktopSettings,
        IAgentAuthService auth,
        AgentOptions options,
        PairingViewModel pairing,
        IUiDispatcher dispatcher,
        ILogger<SetupViewModel> logger)
    {
        _installer = installer;
        _capabilities = capabilities;
        _agentSettings = agentSettings;
        _desktopSettings = desktopSettings;
        _auth = auth;
        _options = options;
        _dispatcher = dispatcher;
        _logger = logger;
        Pairing = pairing;
        var current = agentSettings.Current.ServerBaseUrl;
        _serverUrl = string.IsNullOrWhiteSpace(current) ? options.DefaultServerBaseUrl : current;

        InstallCommand = new AsyncRelayCommand(InstallAsync, CanInstall);
        CancelInstallCommand = new RelayCommand(() => _installCts?.Cancel(), () => Phase == SetupPhase.Installing);
        FinishCommand = new RelayCommand(Finish, () => Phase == SetupPhase.Completed);
        BackToPrerequisitesCommand = new RelayCommand(() => Phase = SetupPhase.Prerequisites, () => Phase == SetupPhase.Pairing && !Pairing.IsBusy);
        Pairing.Paired += OnPaired;
        Pairing.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PairingViewModel.IsBusy))
            {
                BackToPrerequisitesCommand.NotifyCanExecuteChanged();
            }
        };
        LoadSteps();
    }

    /// <summary>Kullanıcı "Bitir" deyince ya da pencere kapanmak istenince tetiklenir.</summary>
    public event EventHandler? CloseRequested;

    /// <summary>Ön gereksinim adımları (kurulum sırasıyla).</summary>
    public ObservableCollection<SetupStepViewModel> Steps { get; } = [];

    /// <summary>Günlük satırları (en yeni sonda).</summary>
    public ObservableCollection<string> Logs { get; } = [];

    /// <summary>Eşleme adımı.</summary>
    public PairingViewModel Pairing { get; }

    /// <summary>Kur ve devam et / Tekrar dene.</summary>
    public IAsyncRelayCommand InstallCommand { get; }

    /// <summary>Kurulumu iptal et.</summary>
    public IRelayCommand CancelInstallCommand { get; }

    /// <summary>Bitir (pencereyi kapatır, uygulama tepside sürer).</summary>
    public IRelayCommand FinishCommand { get; }

    /// <summary>Eşlemeden bileşen listesine dön.</summary>
    public IRelayCommand BackToPrerequisitesCommand { get; }

    /// <summary>Aşama.</summary>
    public SetupPhase Phase
    {
        get => _phase;
        private set
        {
            if (SetProperty(ref _phase, value))
            {
                OnPropertyChanged(nameof(IsPrerequisitesVisible));
                OnPropertyChanged(nameof(IsPairingVisible));
                OnPropertyChanged(nameof(IsCompletedVisible));
                OnPropertyChanged(nameof(IsInstalling));
                OnPropertyChanged(nameof(InstallButtonText));
                OnPropertyChanged(nameof(CurrentStepNumber));
                InstallCommand.NotifyCanExecuteChanged();
                CancelInstallCommand.NotifyCanExecuteChanged();
                FinishCommand.NotifyCanExecuteChanged();
                BackToPrerequisitesCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>Bileşen listesi görünür mü.</summary>
    public bool IsPrerequisitesVisible => Phase is SetupPhase.Prerequisites or SetupPhase.Installing or SetupPhase.Failed;

    /// <summary>Eşleme adımı görünür mü.</summary>
    public bool IsPairingVisible => Phase == SetupPhase.Pairing;

    /// <summary>Bitti ekranı görünür mü.</summary>
    public bool IsCompletedVisible => Phase == SetupPhase.Completed;

    /// <summary>Kurulum sürüyor mu.</summary>
    public bool IsInstalling => Phase == SetupPhase.Installing;

    /// <summary>Üst şeritteki adım numarası (1: bileşenler, 2: eşleme, 3: bitti).</summary>
    public int CurrentStepNumber => Phase switch
    {
        SetupPhase.Pairing => 2,
        SetupPhase.Completed => 3,
        _ => 1,
    };

    /// <summary>Ana düğme metni.</summary>
    public string InstallButtonText => Phase == SetupPhase.Failed
        ? "Tekrar dene"
        : Steps.Any(s => s.NeedsInstall) ? "Kur ve devam et" : "Devam et";

    /// <summary>Genel ilerleme (0–100).</summary>
    public double OverallPercent
    {
        get => _overallPercent;
        private set => SetProperty(ref _overallPercent, Math.Clamp(value, 0, 100));
    }

    /// <summary>Üstteki durum cümlesi.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>GPU/VRAM özeti.</summary>
    public string GpuSummary
    {
        get => _gpuSummary;
        private set => SetProperty(ref _gpuSummary, value);
    }

    /// <summary>Sunucu (API) adresi; kurulum/eşleme başlarken ayarlara kaydedilir.</summary>
    public string ServerUrl
    {
        get => _serverUrl;
        set
        {
            if (SetProperty(ref _serverUrl, value))
            {
                ServerUrlError = null;
                InstallCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>Sunucu adresi hatası (geçersizse).</summary>
    public string? ServerUrlError
    {
        get => _serverUrlError;
        private set => SetProperty(ref _serverUrlError, value);
    }

    /// <summary>
    /// Zorunlu bileşenler zaten kuruluysa bileşen listesini atlayıp doğrudan eşleme adımına geçer
    /// (durum penceresindeki "Eşle" düğmesi için). Değilse hiçbir şey yapmaz.
    /// </summary>
    /// <returns>Eşleme (ya da bitti) adımına geçildiyse <c>true</c>.</returns>
    public bool SkipToPairingIfReady()
    {
        if (Phase != SetupPhase.Prerequisites || !_installer.AreRequiredInstalled() || !TrySaveServerUrl())
        {
            return false;
        }

        EnterPairingOrComplete();
        return true;
    }

    /// <summary>GPU bilgisini algılar (arka planda; hata olursa kısa mesaj).</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public async Task DetectGpuAsync(CancellationToken ct)
    {
        try
        {
            var caps = await _capabilities.DetectAsync(ct).ConfigureAwait(false);
            var summary = DescribeGpus(caps.Gpus);
            _dispatcher.Post(() => GpuSummary = summary);
        }
        catch (OperationCanceledException)
        {
            // Pencere kapandı.
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or System.ComponentModel.Win32Exception)
        {
            _logger.LogWarning(ex, "GPU algılanamadı.");
            _dispatcher.Post(() => GpuSummary = "Ekran kartı algılanamadı.");
        }
    }

    /// <summary>GPU listesini Türkçe tek satıra çevirir (en çok VRAM'li kart; CUDA ve VRAM ile).</summary>
    /// <param name="gpus">GPU'lar.</param>
    /// <returns>Özet.</returns>
    public static string DescribeGpus(IReadOnlyList<GpuInfo> gpus)
    {
        ArgumentNullException.ThrowIfNull(gpus);
        var best = gpus.OrderByDescending(g => g.VramMb).FirstOrDefault();
        if (best is null)
        {
            return "Uygun ekran kartı bulunamadı: yerel ses/görsel motorları işlemciyle (yavaş) çalışır.";
        }

        var gb = (best.VramMb / 1024d).ToString("0.#", CultureInfo.GetCultureInfo("tr-TR"));
        var cuda = best.CudaAvailable ? " · CUDA hazır" : " · CUDA yok";
        return best.Name + " · " + gb + " GB VRAM" + cuda;
    }

    /// <inheritdoc />
    protected override void OnDisposing()
    {
        Pairing.Paired -= OnPaired;
        _installCts?.Cancel();
        _installCts?.Dispose();
        _installCts = null;
        Pairing.Dispose();
    }

    private bool CanInstall() => Phase is SetupPhase.Prerequisites or SetupPhase.Failed && !string.IsNullOrWhiteSpace(ServerUrl);

    private void LoadSteps()
    {
        Steps.Clear();
        foreach (var status in _installer.GetStatus())
        {
            var step = new SetupStepViewModel(status);
            step.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(SetupStepViewModel.IsSelected))
                {
                    OnPropertyChanged(nameof(InstallButtonText));
                }
            };
            Steps.Add(step);
        }

        var missing = Steps.Count(s => !s.IsOptional && !s.IsAlreadyInstalled);
        StatusMessage = missing == 0
            ? "Gerekli bileşenlerin tümü kurulu. İsterseniz isteğe bağlı bileşenleri seçin."
            : missing + " gerekli bileşen kurulacak. Bileşenler yalnızca bu kullanıcının klasörüne kurulur.";
        OverallPercent = Steps.Count == 0 ? 100 : 100d * Steps.Count(s => s.IsAlreadyInstalled) / Steps.Count;
        OnPropertyChanged(nameof(InstallButtonText));
    }

    private async Task InstallAsync()
    {
        if (!TrySaveServerUrl())
        {
            return;
        }

        foreach (var step in Steps)
        {
            if (step.State == SetupStepState.Failed)
            {
                step.State = SetupStepState.Pending;
            }
        }

        var optional = Steps.Where(s => s.IsOptional && s.IsSelected && !s.IsAlreadyInstalled).Select(s => s.Id).ToList();
        var anyWork = Steps.Any(s => s.NeedsInstall);
        _installCts?.Dispose();
        _installCts = new CancellationTokenSource();
        Phase = SetupPhase.Installing;
        StatusMessage = anyWork ? "Bileşenler kuruluyor…" : "Bileşenler denetlendi.";
        AppendLog("Kurulum başladı. Sunucu: " + ServerUrl);

        try
        {
            var progress = new DispatcherProgress<PrerequisiteProgress>(_dispatcher, OnProgress);
            var result = await _installer.InstallAsync(optional, progress, _installCts.Token).ConfigureAwait(true);
            RefreshAfterInstall();
            if (result.IsFailure || Steps.Any(s => s.IsFailed && !s.IsOptional))
            {
                var message = result.IsFailure ? result.Error.Message : "Bazı gerekli bileşenler kurulamadı.";
                _logger.LogWarning("Ön gereksinim kurulumu başarısız: {Message}", message);
                AppendLog("HATA: " + message);
                StatusMessage = "Kurulum tamamlanamadı: " + message;
                Phase = SetupPhase.Failed;
                return;
            }

            if (Steps.Any(s => s.IsFailed))
            {
                AppendLog("Uyarı: bazı isteğe bağlı bileşenler kurulamadı; daha sonra Durum penceresinden kurabilirsiniz.");
            }

            OverallPercent = 100;
            AppendLog("Bileşenler hazır.");
            _logger.LogInformation("Ön gereksinimler hazır.");
            EnterPairingOrComplete();
        }
        catch (OperationCanceledException)
        {
            RefreshAfterInstall();
            AppendLog("Kurulum iptal edildi.");
            StatusMessage = "Kurulum iptal edildi. Tekrar deneyebilirsiniz.";
            Phase = SetupPhase.Failed;
        }
    }

    private bool TrySaveServerUrl()
    {
        var url = (ServerUrl ?? string.Empty).Trim().TrimEnd('/');
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            ServerUrlError = "Geçerli bir adres girin (ör. " + _options.DefaultServerBaseUrl + ").";
            return false;
        }

        if (!string.Equals(_agentSettings.Current.ServerBaseUrl, url, StringComparison.OrdinalIgnoreCase))
        {
            _agentSettings.Update(s => s with { ServerBaseUrl = url });
            _logger.LogInformation("Sunucu adresi kaydedildi: {Url}", url);
        }

        _serverUrl = url;
        OnPropertyChanged(nameof(ServerUrl));
        return true;
    }

    private void OnProgress(PrerequisiteProgress p)
    {
        if (IsDisposed)
        {
            return;
        }

        foreach (var other in Steps.Where(s => s.IsRunning && s.Id != p.StepId))
        {
            other.State = SetupStepState.Done;
            other.Percent = 100;
        }

        var step = Steps.FirstOrDefault(s => s.Id == p.StepId);
        if (step is not null)
        {
            if (!step.IsAlreadyInstalled)
            {
                step.State = SetupStepState.Running;
            }

            step.IsIndeterminate = p.StepPercent is null;
            if (p.StepPercent is { } percent)
            {
                step.Percent = percent;
            }

            step.Detail = p.Message;
        }

        OverallPercent = p.OverallPercent;
        StatusMessage = "(" + (p.StepIndex + 1) + "/" + p.StepCount + ") " + p.StepName + ": " + p.Message;
        if (!string.IsNullOrWhiteSpace(p.LogLine))
        {
            AppendLog(p.LogLine);
        }
        else if (!string.IsNullOrWhiteSpace(p.Message))
        {
            AppendLog(p.StepName + ": " + p.Message);
        }
    }

    private void RefreshAfterInstall()
    {
        var statuses = _installer.GetStatus().ToDictionary(s => s.Id, StringComparer.Ordinal);
        foreach (var step in Steps)
        {
            if (statuses.TryGetValue(step.Id, out var status))
            {
                step.Refresh(status, afterInstallAttempt: true);
            }
        }

        OnPropertyChanged(nameof(InstallButtonText));
    }

    private void EnterPairingOrComplete()
    {
        if (_auth.IsPaired)
        {
            Complete();
            return;
        }

        Phase = SetupPhase.Pairing;
        StatusMessage = "Bu bilgisayarı hesabınızla eşleyin.";
        if (Pairing.StartCommand.CanExecute(null))
        {
            _ = Pairing.StartCommand.ExecuteAsync(null);
        }
    }

    private void OnPaired(object? sender, EventArgs e) => _dispatcher.Post(Complete);

    private void Complete()
    {
        if (IsDisposed)
        {
            return;
        }

        _desktopSettings.Update(s => s with { SetupCompleted = true });
        StatusMessage = "Kurulum tamamlandı. Taha Konsept arka planda (sağ altta, tepside) çalışmaya devam eder.";
        AppendLog("Kurulum tamamlandı.");
        Phase = SetupPhase.Completed;
    }

    private void Finish() => CloseRequested?.Invoke(this, EventArgs.Empty);

    private void AppendLog(string line)
    {
        Logs.Add(DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "  " + line);
        while (Logs.Count > MaxLogLines)
        {
            Logs.RemoveAt(0);
        }
    }
}
