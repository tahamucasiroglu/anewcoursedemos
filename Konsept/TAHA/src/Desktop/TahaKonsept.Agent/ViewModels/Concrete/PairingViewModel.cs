using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Core.Pairing;
using TahaKonsept.Agent.Core.Pairing.Abstract;
using TahaKonsept.Agent.Shell.Abstract;
using TahaKonsept.Agent.Threading.Abstract;
using TahaKonsept.Agent.ViewModels.Base;

namespace TahaKonsept.Agent.ViewModels.Concrete;

/// <summary>
/// Cihaz eşleme adımı: <see cref="StartCommand"/> sunucudan kod alır (<see cref="UserCode"/>, büyük gösterilir) ve onay bekler
/// (<see cref="IDevicePairingService.WaitForApprovalAsync"/>); <see cref="OpenBrowserCommand"/> kodu önceden doldurulmuş onay sayfasını açar;
/// <see cref="CancelCommand"/> beklemeyi bırakır. Eşleme bitince <see cref="Paired"/> tetiklenir. Durum servisten (<see cref="PairingStatus"/>) yansıtılır.
/// </summary>
public sealed class PairingViewModel : ViewModelBase
{
    private readonly IDevicePairingService _pairing;
    private readonly IDesktopLauncher _launcher;
    private readonly IUiDispatcher _dispatcher;
    private readonly ILogger<PairingViewModel> _logger;
    private CancellationTokenSource? _cts;
    private PairingState _state;
    private string _message;
    private string? _userCode;
    private string? _verificationUrl;
    private string? _verificationUrlComplete;
    private bool _isBusy;

    /// <summary>Oluşturur.</summary>
    /// <param name="pairing">Eşleme servisi.</param>
    /// <param name="launcher">Tarayıcı açıcı.</param>
    /// <param name="dispatcher">UI gönderici.</param>
    /// <param name="logger">Log.</param>
    public PairingViewModel(IDevicePairingService pairing, IDesktopLauncher launcher, IUiDispatcher dispatcher, ILogger<PairingViewModel> logger)
    {
        _pairing = pairing;
        _launcher = launcher;
        _dispatcher = dispatcher;
        _logger = logger;
        _state = pairing.Status.State;
        _message = pairing.Status.Message;
        StartCommand = new AsyncRelayCommand(StartAsync, () => !IsBusy);
        OpenBrowserCommand = new RelayCommand(OpenBrowser, () => HasCode);
        CancelCommand = new RelayCommand(Cancel, () => IsBusy);
        _pairing.StatusChanged += OnStatusChanged;
    }

    /// <summary>Eşleme tamamlanınca tetiklenir (UI iş parçacığında).</summary>
    public event EventHandler? Paired;

    /// <summary>Eşlemeyi başlat / yeniden dene.</summary>
    public IAsyncRelayCommand StartCommand { get; }

    /// <summary>"Tarayıcıda onayla".</summary>
    public IRelayCommand OpenBrowserCommand { get; }

    /// <summary>Beklemeyi iptal et.</summary>
    public IRelayCommand CancelCommand { get; }

    /// <summary>Eşleme durumu.</summary>
    public PairingState State
    {
        get => _state;
        private set
        {
            if (SetProperty(ref _state, value))
            {
                OnPropertyChanged(nameof(IsPaired));
                OnPropertyChanged(nameof(IsWaiting));
                OnPropertyChanged(nameof(CanRetry));
                OnPropertyChanged(nameof(StartButtonText));
            }
        }
    }

    /// <summary>Türkçe durum mesajı.</summary>
    public string Message
    {
        get => _message;
        private set => SetProperty(ref _message, value);
    }

    /// <summary>Kullanıcı kodu (<c>XXXX-XXXX</c>).</summary>
    public string? UserCode
    {
        get => _userCode;
        private set
        {
            if (SetProperty(ref _userCode, value))
            {
                OnPropertyChanged(nameof(HasCode));
                OpenBrowserCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>Kodun girileceği adres.</summary>
    public string? VerificationUrl
    {
        get => _verificationUrl;
        private set => SetProperty(ref _verificationUrl, value);
    }

    /// <summary>Kodu önceden doldurulmuş onay adresi.</summary>
    public string? VerificationUrlComplete
    {
        get => _verificationUrlComplete;
        private set => SetProperty(ref _verificationUrlComplete, value);
    }

    /// <summary>İstek/bekleme sürüyor mu.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                StartCommand.NotifyCanExecuteChanged();
                CancelCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(CanRetry));
            }
        }
    }

    /// <summary>Gösterilecek kod var mı.</summary>
    public bool HasCode => !string.IsNullOrWhiteSpace(UserCode) && State == PairingState.WaitingForUser;

    /// <summary>Eşlendi mi.</summary>
    public bool IsPaired => State == PairingState.Paired;

    /// <summary>Kullanıcı onayı bekleniyor mu.</summary>
    public bool IsWaiting => State == PairingState.WaitingForUser;

    /// <summary>Yeniden başlatılabilir mi (meşgul değil ve eşlenmemiş).</summary>
    public bool CanRetry => !IsBusy && !IsPaired;

    /// <summary>Başlat düğmesi metni.</summary>
    public string StartButtonText => State is PairingState.NotPaired or PairingState.Starting ? "Eşleme kodu al" : "Yeni kod al";

    /// <summary>Eşleme sırasında kullanılan sunucu adresini değiştirme gibi dış etkenler için durumu servisten yeniden okur.</summary>
    public void SyncFromService() => Apply(_pairing.Status);

    /// <inheritdoc />
    protected override void OnDisposing()
    {
        _pairing.StatusChanged -= OnStatusChanged;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async Task StartAsync()
    {
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        IsBusy = true;
        VerificationUrlComplete = null;
        try
        {
            var start = await _pairing.StartAsync(ct).ConfigureAwait(true);
            if (start.IsFailure)
            {
                _logger.LogWarning("Eşleme başlatılamadı: {Error}", start.Error.Message);
                Apply(_pairing.Status);
                return;
            }

            var response = start.Value;
            UserCode = response.UserCode;
            VerificationUrl = response.VerificationUrl;
            VerificationUrlComplete = string.IsNullOrWhiteSpace(response.VerificationUrlComplete) ? response.VerificationUrl : response.VerificationUrlComplete;
            State = PairingState.WaitingForUser;
            OnPropertyChanged(nameof(HasCode));
            OpenBrowserCommand.NotifyCanExecuteChanged();

            var result = await _pairing.WaitForApprovalAsync(response, ct).ConfigureAwait(true);
            Apply(_pairing.Status);
            if (result.IsSuccess)
            {
                State = PairingState.Paired;
                _logger.LogInformation("Cihaz eşlendi.");
                Paired?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                _logger.LogWarning("Eşleme tamamlanmadı: {Error}", result.Error.Message);
            }
        }
        catch (OperationCanceledException)
        {
            State = PairingState.Cancelled;
            Message = "Eşleme iptal edildi.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OpenBrowser()
    {
        var url = VerificationUrlComplete ?? VerificationUrl;
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        var result = _launcher.OpenUrl(url);
        if (result.IsFailure)
        {
            Message = result.Error.Message;
        }
    }

    private void Cancel() => _cts?.Cancel();

    private void OnStatusChanged(object? sender, PairingStatus status) => _dispatcher.Post(() => Apply(status));

    private void Apply(PairingStatus status)
    {
        if (IsDisposed)
        {
            return;
        }

        State = status.State;
        Message = status.Message;
        if (!string.IsNullOrWhiteSpace(status.UserCode))
        {
            UserCode = status.UserCode;
        }

        if (!string.IsNullOrWhiteSpace(status.VerificationUrl))
        {
            VerificationUrl = status.VerificationUrl;
        }

        if (status.State is not PairingState.WaitingForUser)
        {
            OnPropertyChanged(nameof(HasCode));
            OpenBrowserCommand.NotifyCanExecuteChanged();
        }
    }
}
