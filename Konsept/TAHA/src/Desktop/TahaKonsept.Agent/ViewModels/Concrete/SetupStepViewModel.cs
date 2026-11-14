using TahaKonsept.Agent.Core.Prerequisites;
using TahaKonsept.Agent.ViewModels.Base;

namespace TahaKonsept.Agent.ViewModels.Concrete;

/// <summary>
/// Kurulum penceresindeki bir ön gereksinim satırı: ad, açıklama, grup, isteğe bağlılık (<see cref="IsSelected"/> onay kutusu),
/// <see cref="State"/>, adım yüzdesi (<see cref="Percent"/>; bilinmiyorsa <see cref="IsIndeterminate"/>) ve Türkçe <see cref="StatusText"/>.
/// </summary>
public sealed class SetupStepViewModel : ViewModelBase
{
    private SetupStepState _state;
    private double _percent;
    private bool _isIndeterminate;
    private bool _isSelected;
    private string? _detail;
    private string? _installedVersion;

    /// <summary>Durum kaydından oluşturur.</summary>
    /// <param name="status">Bileşen durumu.</param>
    public SetupStepViewModel(PrerequisiteStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        Id = status.Id;
        DisplayName = status.DisplayName;
        Description = status.Description;
        Group = status.Group;
        IsOptional = status.IsOptional;
        Refresh(status);
    }

    /// <summary>Bileşen kimliği.</summary>
    public string Id { get; }

    /// <summary>Ad.</summary>
    public string DisplayName { get; }

    /// <summary>Açıklama.</summary>
    public string Description { get; }

    /// <summary>Grup (ör. Temel, Python + CUDA).</summary>
    public string Group { get; }

    /// <summary>İsteğe bağlı mı.</summary>
    public bool IsOptional { get; }

    /// <summary>İsteğe bağlı bileşen kurulsun mu (zorunlularda her zaman <c>true</c>).</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (!IsOptional || IsAlreadyInstalled)
            {
                return;
            }

            if (SetProperty(ref _isSelected, value) && State is SetupStepState.Pending or SetupStepState.Skipped)
            {
                State = value ? SetupStepState.Pending : SetupStepState.Skipped;
            }
        }
    }

    /// <summary>Seçim kutusu değiştirilebilir mi (isteğe bağlı ve kurulu değil).</summary>
    public bool CanToggle => IsOptional && !IsAlreadyInstalled;

    /// <summary>Durum.</summary>
    public SetupStepState State
    {
        get => _state;
        set
        {
            if (SetProperty(ref _state, value))
            {
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(IsAlreadyInstalled));
                OnPropertyChanged(nameof(CanToggle));
                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(IsFailed));
                OnPropertyChanged(nameof(IsOk));
            }
        }
    }

    /// <summary>Adım yüzdesi (0–100).</summary>
    public double Percent
    {
        get => _percent;
        set => SetProperty(ref _percent, Math.Clamp(value, 0, 100));
    }

    /// <summary>Yüzde bilinmiyor mu (belirsiz ilerleme çubuğu).</summary>
    public bool IsIndeterminate
    {
        get => _isIndeterminate;
        set => SetProperty(ref _isIndeterminate, value);
    }

    /// <summary>Son ilerleme mesajı.</summary>
    public string? Detail
    {
        get => _detail;
        set => SetProperty(ref _detail, value);
    }

    /// <summary>Kurulu sürüm.</summary>
    public string? InstalledVersion
    {
        get => _installedVersion;
        private set => SetProperty(ref _installedVersion, value);
    }

    /// <summary>Kurulu (önceden ya da bu oturumda).</summary>
    public bool IsAlreadyInstalled => State is SetupStepState.Installed or SetupStepState.Done;

    /// <summary>Kuruluyor mu.</summary>
    public bool IsRunning => State == SetupStepState.Running;

    /// <summary>Başarısız mı.</summary>
    public bool IsFailed => State == SetupStepState.Failed;

    /// <summary>Hazır mı (yeşil işaret).</summary>
    public bool IsOk => IsAlreadyInstalled;

    /// <summary>Kurulumda bu adım işlenecek mi (zorunlu ya da seçili ve kurulu değil).</summary>
    public bool NeedsInstall => !IsAlreadyInstalled && (!IsOptional || IsSelected);

    /// <summary>Türkçe durum metni.</summary>
    public string StatusText => State switch
    {
        SetupStepState.Installed => string.IsNullOrWhiteSpace(InstalledVersion) ? "Kurulu" : "Kurulu (" + InstalledVersion + ")",
        SetupStepState.Running => "Kuruluyor…",
        SetupStepState.Done => "Kuruldu",
        SetupStepState.Failed => "Kurulamadı",
        SetupStepState.Skipped => "Atlanacak (isteğe bağlı)",
        _ => IsOptional ? "Kurulacak (isteğe bağlı)" : "Kurulacak",
    };

    /// <summary>Kurulum öncesi/sonrası durum kaydıyla satırı günceller.</summary>
    /// <param name="status">Bileşen durumu.</param>
    /// <param name="afterInstallAttempt">Bir kurulum denemesinden sonra mı (kurulu değilse ve denendiyse başarısız sayılır).</param>
    public void Refresh(PrerequisiteStatus status, bool afterInstallAttempt = false)
    {
        ArgumentNullException.ThrowIfNull(status);
        InstalledVersion = status.InstalledVersion;
        IsIndeterminate = false;
        if (status.IsInstalled)
        {
            var installedNow = State == SetupStepState.Done || (afterInstallAttempt && State != SetupStepState.Installed);
            State = installedNow ? SetupStepState.Done : SetupStepState.Installed;
            Percent = 100;
            _isSelected = IsOptional;
            OnPropertyChanged(nameof(IsSelected));
            return;
        }

        if (afterInstallAttempt && NeedsInstall)
        {
            State = SetupStepState.Failed;
            return;
        }

        Percent = 0;
        if (!IsOptional)
        {
            _isSelected = true;
            OnPropertyChanged(nameof(IsSelected));
            State = SetupStepState.Pending;
        }
        else
        {
            State = _isSelected ? SetupStepState.Pending : SetupStepState.Skipped;
        }
    }
}
