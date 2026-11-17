using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Threading;
using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Resources;

namespace Konsept.Agent;

/// <summary>
/// Ajan güncelleme ekranı (spec 0023 Req 2, 3, 5, 7, 14): "Şu an bunları güncelliyoruz" başlığı, ajan satırı (kurulu → yeni sürüm, durum metni),
/// ilerleme, akan satırlar. Güncelleme yalnızca <b>Şimdi güncelle</b> ile başlar; "Üretiliyor" iş varken bekleme metni görünür ve iş bitince
/// kendiliğinden sürer. Hata satırında <b>Tekrar dene</b> (yalnızca bu satır) ve <b>Hatayı bildir</b> (0022, bileşen + adım). Pencereyi kapatmak
/// güncellemeyi iptal etmez. Tüm kontroller klavyeyle kullanılabilir ve erişilebilir ada sahiptir.
/// </summary>
public sealed partial class UpdateWindow : Window
{
    private readonly ObservableCollection<string> _lines = [];
    private IAgentUpdater? _updater;
    private IErrorReportCenter? _reports;
    private CancellationToken _stop;
    private UpdateEvent _last = new(UpdateStatus.Idle);

    /// <summary>Initializes a new instance of the <see cref="UpdateWindow"/> class.</summary>
    public UpdateWindow()
    {
        InitializeComponent();
        Title = AgentText.UpdateTitle;
        HeadingText.Text = AgentText.UpdateHeading;
        IntroText.Text = AgentText.UpdateIntro;
        RowName.Text = AgentText.UpdateAgentName;
        LogLabel.Text = AgentText.SetupLogName;
        AutomationProperties.SetName(LogList, AgentText.SetupLogName);
        AutomationProperties.SetName(Progress, AgentText.SetupProgressName);
        Label(StartButton, AgentText.UpdateNow);
        Label(CloseButton, AgentText.UpdateClose);
        Label(RetryButton, AgentText.SetupRetry);
        Label(ReportButton, AgentText.SetupReport);
        LogList.ItemsSource = _lines;
        StartButton.Click += (_, _) => _ = StartAsync();
        RetryButton.Click += (_, _) => _ = StartAsync();
        ReportButton.Click += (_, _) => ReportFailure();
        CloseButton.Click += (_, _) => Hide();
    }

    /// <summary>Gets süren güncelleme görevi; yoksa <c>null</c>.</summary>
    internal Task<bool>? Running { get; private set; }

    /// <summary>Gets a value indicating whether "Şimdi güncelle" görünür ve etkin.</summary>
    internal bool StartEnabled => StartButton.IsVisible && StartButton.IsEnabled;

    /// <summary>Gets satırın durum metni.</summary>
    internal string RowStatusText => RowStatus.Text ?? string.Empty;

    /// <summary>Gets satır ayrıntısı (kurulu → yeni sürüm).</summary>
    internal string RowDetailText => RowDetail.Text ?? string.Empty;

    /// <summary>Gets ileti metni (bekleme, doğrulanamadı, tamamlandı).</summary>
    internal string Message => MessageText.Text ?? string.Empty;

    /// <summary>Gets başlık metni.</summary>
    internal string Heading => HeadingText.Text ?? string.Empty;

    /// <summary>Gets akan satırlar.</summary>
    internal IReadOnlyList<string> Lines => _lines;

    /// <summary>Gets satırın görünür düğmeleri.</summary>
    internal IReadOnlyList<string> RowButtons =>
        [.. new[] { RetryButton, ReportButton }.Where(b => b.IsVisible).Select(b => (string)b.Content!)];

    /// <summary>Gets tüm etkileşimli kontrollerin erişilebilir adları.</summary>
    internal IReadOnlyList<string> AccessibleNames =>
        [.. new Control[] { StartButton, CloseButton, RetryButton, ReportButton, Progress, LogList, AgentRow }
            .Select(c => AutomationProperties.GetName(c) ?? string.Empty)];

    /// <summary>Pencereyi güncelleyiciye bağlar.</summary>
    /// <param name="updater">Güncelleyici (önceden denetlenmiş; <see cref="IAgentUpdater.Available"/> dolu).</param>
    /// <param name="reports">Hata bildirimi merkezi; yoksa "Hatayı bildir" görünmez.</param>
    /// <param name="stop">Uygulama kapanınca güncellemeyi durduran belirteç.</param>
    internal void Attach(IAgentUpdater updater, IErrorReportCenter? reports, CancellationToken stop)
    {
        _updater = updater ?? throw new ArgumentNullException(nameof(updater));
        _reports = reports;
        _stop = stop;
        updater.Changed += (_, e) => Dispatcher.UIThread.Post(() => Apply(e));
        Apply(updater.Current.Status == UpdateStatus.Idle && updater.Available is { } available
            ? new UpdateEvent(UpdateStatus.Available, Version: available.Version)
            : updater.Current);
    }

    /// <summary>"Şimdi güncelle" / "Tekrar dene": güncellemeyi başlatır (kullanıcı onayı).</summary>
    /// <returns>Görev.</returns>
    internal async Task StartAsync()
    {
        if (_updater is null || Running is not null || _updater.Available is null)
        {
            return;
        }

        Running = _updater.ApplyAsync(_stop);
        Refresh();
        try
        {
            await Running.ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            // Uygulama kapanıyor.
        }
        finally
        {
            Running = null;
            Refresh();
        }
    }

    /// <summary>"Hatayı bildir": 0022 akışını bileşen adı ve adımla açar.</summary>
    internal void ReportFailure()
    {
        if (_reports is null || _last.Status != UpdateStatus.Failed)
        {
            return;
        }

        _reports.Raise(
            string.Format(CultureInfo.CurrentCulture, AgentText.UpdateReportSummary, AgentText.UpdateAgentName, FailureText(_last.Failure)),
            AgentText.UpdateAgentName,
            _last.Failure == SetupFailure.InstallFailed ? InstallStep.Install : InstallStep.Download);
    }

    /// <inheritdoc />
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (e.CloseReason != WindowCloseReason.ApplicationShutdown)
        {
            e.Cancel = true; // Kapatmak ajanı ve süren güncellemeyi durdurmaz; pencere tray'den yeniden açılır.
            Hide();
        }

        base.OnClosing(e);
    }

    private static void Label(Button button, string text)
    {
        button.Content = text;
        AutomationProperties.SetName(button, text);
    }

    private static string FailureText(SetupFailure failure) => failure switch
    {
        SetupFailure.NoInternet => AgentText.FailureNoInternet,
        SetupFailure.DownloadFailed => AgentText.FailureDownload,
        SetupFailure.DiskFull => AgentText.FailureDisk,
        _ => AgentText.FailureInstall,
    };

    private void Apply(UpdateEvent e)
    {
        var previous = _last.Status;
        _last = e;
        var version = e.Version ?? _updater?.Available?.Version ?? string.Empty;
        RowDetail.Text = string.Format(CultureInfo.CurrentCulture, AgentText.UpdateRowDetail, _updater?.CurrentVersion, version);
        var failed = e.Status == UpdateStatus.Failed;
        RowStatus.Text = e.Status switch
        {
            UpdateStatus.Downloading => string.Format(CultureInfo.CurrentCulture, AgentText.UpdateStatusDownloading, e.Percent),
            UpdateStatus.Verifying => AgentText.UpdateStatusVerifying,
            UpdateStatus.Installing => AgentText.UpdateStatusInstalling,
            UpdateStatus.Completed => AgentText.UpdateStatusDone,
            UpdateStatus.Failed => string.Format(CultureInfo.CurrentCulture, AgentText.StatusFailed, FailureText(e.Failure)),
            _ => AgentText.UpdateStatusWaiting,
        };
        AgentRow.Classes.Set("error", failed);
        AgentRow.Bind(Border.BorderBrushProperty, AgentRow.GetResourceObservable(failed ? "KonseptErrorBrush" : "KonseptBorderBrush"));
        RowStatus.Bind(TextBlock.ForegroundProperty, RowStatus.GetResourceObservable(failed ? "KonseptErrorBrush" : "KonseptWindowForeground"));
        AutomationProperties.SetName(AgentRow, $"{AgentText.UpdateAgentName}: {RowStatus.Text}");
        Progress.Value = e.Status is UpdateStatus.Downloading ? e.Percent : e.Status is UpdateStatus.Verifying or UpdateStatus.Installing or UpdateStatus.Completed ? 100 : 0;
        MessageText.Text = e.Status switch
        {
            UpdateStatus.Available => string.Format(CultureInfo.CurrentCulture, AgentText.UpdateAvailable, version),
            UpdateStatus.WaitingForProduction => AgentText.UpdateWaitingProduction,
            UpdateStatus.VerificationFailed => AgentText.UpdateVerificationFailed,
            UpdateStatus.Completed => AgentText.UpdateDone,
            _ => string.Empty,
        };
        if (e.Status != previous && LineFor(e) is { } line)
        {
            _lines.Add(line);
            LogList.ScrollIntoView(line);
        }

        Refresh();
    }

    private string? LineFor(UpdateEvent e) => e.Status switch
    {
        UpdateStatus.WaitingForProduction => AgentText.UpdateWaitingProduction,
        UpdateStatus.Downloading => AgentText.UpdateLineStarted,
        UpdateStatus.Verifying => AgentText.UpdateStatusVerifying,
        UpdateStatus.Installing => AgentText.UpdateStatusInstalling,
        UpdateStatus.Completed => AgentText.UpdateDone,
        UpdateStatus.Failed => RowStatus.Text,
        UpdateStatus.VerificationFailed => AgentText.UpdateVerificationFailed,
        _ => null,
    };

    private void Refresh()
    {
        var busy = Running is not null;
        var failed = _last.Status == UpdateStatus.Failed;
        StartButton.IsVisible = _last.Status is UpdateStatus.Available or UpdateStatus.Idle;
        StartButton.IsEnabled = !busy && _updater?.Available is not null;
        RetryButton.IsVisible = failed;
        RetryButton.IsEnabled = !busy;
        ReportButton.IsVisible = failed && _reports is not null;
    }
}
