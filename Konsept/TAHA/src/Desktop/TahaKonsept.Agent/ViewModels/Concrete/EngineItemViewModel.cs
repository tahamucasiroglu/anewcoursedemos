using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Abstractions.Engines;
using TahaKonsept.Agent.EngineList;
using TahaKonsept.Agent.Threading;
using TahaKonsept.Agent.Threading.Abstract;
using TahaKonsept.Agent.ViewModels.Base;

namespace TahaKonsept.Agent.ViewModels.Concrete;

/// <summary>
/// Durum penceresindeki bir motor satırı: ad, grup, VRAM etiketi (<see cref="VramText"/>; kartın VRAM'i yetmiyorsa <see cref="IsVramInsufficient"/>),
/// kurulu mu (<see cref="IsInstalled"/>) ve <see cref="InstallCommand"/> ("Kur": <c>IAgentEngine.EnsureReadyAsync</c>, ilerleme çubuklu).
/// </summary>
public sealed class EngineItemViewModel : ViewModelBase
{
    private readonly EngineDescriptor _descriptor;
    private readonly IUiDispatcher _dispatcher;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _cts = new();
    private bool _isInstalled;
    private bool _isBusy;
    private double _percent;
    private bool _isIndeterminate;
    private string? _message;
    private long? _gpuVramMb;

    /// <summary>Oluşturur.</summary>
    /// <param name="descriptor">Motor.</param>
    /// <param name="dispatcher">UI gönderici.</param>
    /// <param name="logger">Log.</param>
    public EngineItemViewModel(EngineDescriptor descriptor, IUiDispatcher dispatcher, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _descriptor = descriptor;
        _dispatcher = dispatcher;
        _logger = logger;
        InstallCommand = new AsyncRelayCommand(InstallAsync, () => !IsBusy && !IsInstalled);
        RefreshInstalled();
    }

    /// <summary>Katalog anahtarı.</summary>
    public string Key => _descriptor.Engine.Key;

    /// <summary>Ad.</summary>
    public string DisplayName => _descriptor.Engine.DisplayName;

    /// <summary>Grup.</summary>
    public EngineCategory Category => _descriptor.Category;

    /// <summary>Grup adı (Türkçe).</summary>
    public string CategoryText => Category switch
    {
        EngineCategory.Video => "Video",
        EngineCategory.Speech => "Ses",
        _ => "Görsel",
    };

    /// <summary>VRAM etiketi (ör. <c>4 GB VRAM</c>, GPU gerekmiyorsa <c>GPU gerekmez</c>).</summary>
    public string VramText => _descriptor.Engine.MinVramMb is { } mb
        ? (mb / 1024d).ToString("0.#", CultureInfo.GetCultureInfo("tr-TR")) + " GB VRAM"
        : "GPU gerekmez";

    /// <summary>Bilgisayarın VRAM'i bu motor için yetersiz mi.</summary>
    public bool IsVramInsufficient => _descriptor.Engine.MinVramMb is { } need && _gpuVramMb is { } have && have < need;

    /// <summary>Kurulu mu.</summary>
    public bool IsInstalled
    {
        get => _isInstalled;
        private set
        {
            if (SetProperty(ref _isInstalled, value))
            {
                OnPropertyChanged(nameof(StatusText));
                InstallCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>Kuruluyor mu.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(StatusText));
                InstallCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>Kurulum yüzdesi.</summary>
    public double Percent
    {
        get => _percent;
        private set => SetProperty(ref _percent, Math.Clamp(value, 0, 100));
    }

    /// <summary>Yüzde bilinmiyor mu.</summary>
    public bool IsIndeterminate
    {
        get => _isIndeterminate;
        private set => SetProperty(ref _isIndeterminate, value);
    }

    /// <summary>Son mesaj (ilerleme ya da hata).</summary>
    public string? Message
    {
        get => _message;
        private set => SetProperty(ref _message, value);
    }

    /// <summary>Durum metni.</summary>
    public string StatusText => IsBusy ? "Kuruluyor…" : IsInstalled ? "Kurulu" : "Kurulu değil";

    /// <summary>"Kur" komutu.</summary>
    public IAsyncRelayCommand InstallCommand { get; }

    /// <summary>Bilgisayarın en yüksek VRAM'ini bildirir (yetersizlik uyarısı için).</summary>
    /// <param name="vramMb">VRAM (MB) ya da GPU yoksa <c>null</c>.</param>
    public void SetGpuVram(long? vramMb)
    {
        _gpuVramMb = vramMb;
        OnPropertyChanged(nameof(IsVramInsufficient));
    }

    /// <summary>Kurulu bilgisini motordan yeniden okur.</summary>
    public void RefreshInstalled()
    {
        try
        {
            IsInstalled = _descriptor.Engine.IsInstalled();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Motor durumu okunamadı: {Key}", Key);
            IsInstalled = false;
        }
    }

    /// <inheritdoc />
    protected override void OnDisposing()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    private async Task InstallAsync()
    {
        IsBusy = true;
        Percent = 0;
        IsIndeterminate = true;
        Message = "Hazırlanıyor…";
        _logger.LogInformation("Motor kuruluyor: {Key}", Key);
        try
        {
            var progress = new DispatcherProgress<EngineSetupProgress>(_dispatcher, p =>
            {
                Message = p.Message;
                IsIndeterminate = p.Percent is null;
                if (p.Percent is { } percent)
                {
                    Percent = percent;
                }
            });
            var result = await _descriptor.Engine.EnsureReadyAsync(progress, _cts.Token).ConfigureAwait(true);
            if (result.IsFailure)
            {
                Message = "Kurulamadı: " + result.Error.Message;
                _logger.LogWarning("Motor kurulamadı: {Key}: {Error}", Key, result.Error.Message);
            }
            else
            {
                Message = "Hazır.";
                Percent = 100;
                _logger.LogInformation("Motor hazır: {Key}", Key);
            }
        }
        catch (OperationCanceledException)
        {
            Message = "İptal edildi.";
        }
        finally
        {
            IsIndeterminate = false;
            IsBusy = false;
            if (!IsDisposed)
            {
                RefreshInstalled();
            }
        }
    }
}
