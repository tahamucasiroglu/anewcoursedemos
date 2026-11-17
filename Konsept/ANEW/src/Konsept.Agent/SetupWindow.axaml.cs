using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Resources;

namespace Konsept.Agent;

/// <summary>
/// İlk kurulum ekranı (spec 0009). Bileşen satırları ad, amaç, lisans, indirme boyutu ve durumu metin olarak gösterir (yalnızca renk değil);
/// Hata satırında "Tekrar dene" ve "Hatayı bildir" (0022 akışı, bileşen + adım), Onarılmalı satırında "Onar". "Kurulumu başlat" lisans
/// onayı ve ön kontrol (internet, disk) geçmeden pasiftir. Kurulum sürerken pencere kapatılırsa arka planda devam onayı sorulur.
/// Hazır olunca <see cref="Completed"/> yükselir. Tüm kontroller klavyeyle kullanılabilir ve erişilebilir ada sahiptir.
/// </summary>
public sealed partial class SetupWindow : Window
{
    private const double Gigabyte = 1024d * 1024 * 1024;
    private readonly Dictionary<string, (Border Row, TextBlock Status, Button Retry, Button Report, Button Repair)> _rows = new(StringComparer.Ordinal);
    private readonly ObservableCollection<string> _lines = [];
    private ISetupCoordinator? _setup;
    private IErrorReportCenter? _reports;
    private CancellationToken _stop;
    private Func<Task<string?>>? _pickFolder;
    private bool _closing;
    private bool _updating;

    /// <summary>Initializes a new instance of the <see cref="SetupWindow"/> class.</summary>
    public SetupWindow()
    {
        InitializeComponent();
        Title = AgentText.SetupTitle;
        HeadingText.Text = AgentText.SetupHeading;
        IntroText.Text = AgentText.SetupIntro;
        VideoLabel.Text = AgentText.SetupVideoFolder;
        AutomationProperties.SetName(VideoFolderBox, AgentText.SetupVideoFolder);
        VideoErrorText.Text = AgentText.SetupVideoInvalid;
        ComponentsLabel.Text = AgentText.SetupComponentsLabel;
        LicenseText.Text = AgentText.SetupLicense;
        LogLabel.Text = AgentText.SetupLogName;
        AutomationProperties.SetName(LogList, AgentText.SetupLogName);
        AutomationProperties.SetName(Progress, AgentText.SetupProgressName);
        ConfirmText.Text = AgentText.SetupConfirm;
        Label(VideoChangeButton, AgentText.SetupVideoChange);
        Label(StartButton, AgentText.SetupStart);
        Label(YesButton, AgentText.SetupYes);
        Label(NoButton, AgentText.SetupNo);
        AcceptBox.Content = AgentText.SetupAccept;
        AutomationProperties.SetName(AcceptBox, AgentText.SetupAccept);
        LogList.ItemsSource = _lines;
        AcceptBox.IsCheckedChanged += (_, _) => Refresh();
        StartButton.Click += (_, _) => _ = StartAsync();
        VideoChangeButton.Click += (_, _) => _ = ChangeVideoFolderAsync();
        VideoFolderBox.LostFocus += (_, _) => SetVideoFolder(VideoFolderBox.Text ?? string.Empty);
        YesButton.Click += (_, _) => AnswerBackground(true);
        NoButton.Click += (_, _) => AnswerBackground(false);
    }

    /// <summary>Kurulum tamamlandı · Hazır.</summary>
    public event EventHandler? Completed;

    /// <summary>Kurulum arka plana alındı (pencere gizlendi).</summary>
    public event EventHandler? MovedToBackground;

    /// <summary>Gets süren kurulum görevi; yoksa <c>null</c> (tray "Çıkış" bekler).</summary>
    internal Task? Running { get; private set; }

    /// <summary>Gets a value indicating whether "Kurulumu başlat" etkin.</summary>
    internal bool StartEnabled => StartButton.IsEnabled;

    /// <summary>Gets o an işlenen bileşen metni.</summary>
    internal string CurrentDisplay => CurrentText.Text ?? string.Empty;

    /// <summary>Gets ön kontrol metni ("Gereken: X GB, Boş: Y GB").</summary>
    internal string SpaceDisplay => SpaceText.Text ?? string.Empty;

    /// <summary>Gets başlatmama sebebi.</summary>
    internal string ReasonDisplay => ReasonText.Text ?? string.Empty;

    /// <summary>Gets tamamlanma metni.</summary>
    internal string DoneDisplay => DoneText.Text ?? string.Empty;

    /// <summary>Gets ekran başlığı (kurulum ya da güncelleme).</summary>
    internal string HeadingDisplay => HeadingText.Text ?? string.Empty;

    /// <summary>Gets listelenen bileşen kimlikleri.</summary>
    internal IReadOnlyList<string> ListedComponents => [.. _rows.Keys];

    /// <summary>Gets akan satırlar.</summary>
    internal IReadOnlyList<string> Lines => _lines;

    /// <summary>Gets a value indicating whether video klasörü hata metni görünür.</summary>
    internal bool VideoErrorVisible => VideoErrorText.IsVisible;

    /// <summary>Gets gösterilen video klasörü.</summary>
    internal string VideoFolderDisplay => VideoFolderBox.Text ?? string.Empty;

    /// <summary>Gets a value indicating whether arka plan onayı görünür.</summary>
    internal bool ConfirmVisible => ConfirmPanel.IsVisible;

    /// <summary>Gets ilerleme değeri.</summary>
    internal double ProgressValue => Progress.Value;

    /// <summary>Gets tüm etkileşimli kontrollerin erişilebilir adları (testler okur).</summary>
    internal IReadOnlyList<string> AccessibleNames =>
        [.. new Control[] { VideoFolderBox, VideoChangeButton, AcceptBox, StartButton, Progress, LogList, YesButton, NoButton }
            .Select(c => AutomationProperties.GetName(c) ?? string.Empty)];

    /// <summary>Pencereyi kurulum motoruna bağlar.</summary>
    /// <param name="setup">Kurulum motoru (önceden <c>InspectAsync</c> çalışmış).</param>
    /// <param name="reports">Hata bildirimi merkezi (0022); yoksa "Hatayı bildir" görünmez.</param>
    /// <param name="stop">Tray "Çıkış"ta kurulumu durduran belirteç.</param>
    /// <param name="pickFolder">Klasör seçici (testte sahte).</param>
    internal void Attach(ISetupCoordinator setup, IErrorReportCenter? reports, CancellationToken stop, Func<Task<string?>>? pickFolder = null)
    {
        _setup = setup ?? throw new ArgumentNullException(nameof(setup));
        _reports = reports;
        _stop = stop;
        _pickFolder = pickFolder ?? PickFolderWithDialogAsync;
        InstallRootText.Text = string.Format(CultureInfo.CurrentCulture, AgentText.SetupInstallRoot, setup.InstallRoot) + Environment.NewLine
            + (setup.Gpu is { } gpu
                ? string.Format(CultureInfo.CurrentCulture, AgentText.SetupGpu, gpu.Name, (gpu.VramMb / 1024d).ToString("0.#", CultureInfo.CurrentCulture))
                : AgentText.SetupGpuNone);
        VideoFolderBox.Text = setup.VideoFolder;
        ComponentList.Children.Clear();
        _rows.Clear();

        // Güncelleme (spec 0023 Req 5): kurulu bileşen varken ekran "Şu an bunları güncelliyoruz" başlığıyla açılır ve yalnızca kurulmamış/değişen
        // bileşenleri listeler (yeni ajan sürümü yeni bileşen sürümleri getirdiğinde).
        _updating = setup.Components.Any(s => s.Status == ComponentStatus.Installed);
        if (_updating)
        {
            Title = AgentText.UpdateTitle;
            HeadingText.Text = AgentText.UpdateHeading;
            IntroText.Text = AgentText.UpdateIntro;
        }

        foreach (var state in setup.Components.Where(s => !_updating || s.Status != ComponentStatus.Installed))
        {
            AddRow(state);
        }

        setup.Changed += (_, e) => Dispatcher.UIThread.Post(() => Apply(e));
        Refresh();
    }

    /// <summary>"Kurulumu başlat".</summary>
    /// <returns>Görev.</returns>
    internal Task StartAsync()
    {
        if (_setup is null || !StartButton.IsEnabled)
        {
            return Task.CompletedTask;
        }

        return Track(_setup.RunAsync(_stop));
    }

    /// <summary>"Tekrar dene" / "Onar": yalnızca bu bileşen.</summary>
    /// <param name="componentId">Bileşen.</param>
    /// <returns>Görev.</returns>
    internal Task RetryAsync(string componentId) =>
        _setup is null || _setup.IsRunning ? Task.CompletedTask : Track(_setup.RetryAsync(componentId, _stop));

    /// <summary>"Hatayı bildir": 0022 ajan hata bildirimi akışını bileşen adı ve hatanın adımıyla açar.</summary>
    /// <param name="componentId">Bileşen.</param>
    internal void ReportFailure(string componentId)
    {
        var state = _setup?.Components.FirstOrDefault(c => c.Component.Id == componentId);
        if (_reports is null || state is not { Status: ComponentStatus.Failed })
        {
            return;
        }

        _reports.Raise(
            string.Format(CultureInfo.CurrentCulture, AgentText.SetupReportSummary, state.Component.Name, FailureText(state)),
            state.Component.Name,
            state.FailedStep ?? InstallStep.Install);
    }

    /// <summary>Video klasörünü değiştirir; geçersizse hata metni gösterir ve eski değeri korur.</summary>
    /// <param name="folder">Klasör.</param>
    /// <returns>Geçerli mi.</returns>
    internal bool SetVideoFolder(string folder)
    {
        if (_setup is null)
        {
            return false;
        }

        var ok = _setup.TrySetVideoFolder(folder);
        VideoErrorText.IsVisible = !ok;
        VideoFolderBox.Text = ok ? _setup.VideoFolder : folder;
        return ok;
    }

    /// <summary>Satırın durum metni (testler okur).</summary>
    /// <param name="componentId">Bileşen.</param>
    /// <returns>Metin.</returns>
    internal string RowStatus(string componentId) => _rows[componentId].Status.Text ?? string.Empty;

    /// <summary>Satırın görünür düğmeleri (testler okur).</summary>
    /// <param name="componentId">Bileşen.</param>
    /// <returns>Düğme metinleri.</returns>
    internal IReadOnlyList<string> RowButtons(string componentId)
    {
        var row = _rows[componentId];
        return [.. new[] { row.Retry, row.Report, row.Repair }.Where(b => b.IsVisible).Select(b => (string)b.Content!)];
    }

    /// <summary>Satırın erişilebilir adı (durumu metin olarak içerir).</summary>
    /// <param name="componentId">Bileşen.</param>
    /// <returns>Ad.</returns>
    internal string RowAccessibleName(string componentId) => AutomationProperties.GetName(_rows[componentId].Row) ?? string.Empty;

    /// <summary>Satır hata renginde mi (testler okur).</summary>
    /// <param name="componentId">Bileşen.</param>
    /// <returns>Hata fırçası kullanılıyor mu.</returns>
    internal bool RowIsError(string componentId) => _rows[componentId].Row.Classes.Contains("error");

    /// <summary>Arka plan onayının cevabı.</summary>
    /// <param name="yes">Evet → pencere gizlenir, kurulum sürer; Hayır → pencere açık kalır.</param>
    internal void AnswerBackground(bool yes)
    {
        ConfirmPanel.IsVisible = false;
        if (yes)
        {
            Hide();
            MovedToBackground?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Kurulum bitince pencereyi gerçekten kapatır.</summary>
    internal void CloseForGood()
    {
        _closing = true;
        Close();
    }

    /// <inheritdoc />
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!_closing && e.CloseReason != WindowCloseReason.ApplicationShutdown)
        {
            e.Cancel = true;
            if (_setup?.IsRunning == true)
            {
                ConfirmPanel.IsVisible = true;
                YesButton.Focus();
            }
            else
            {
                // Pencereyi kapatmak ajanı kapatmaz (0001 AC-5); kurulum ekranı tray'den yeniden açılır.
                Hide();
            }
        }

        base.OnClosing(e);
    }

    private static void Label(Button button, string text)
    {
        button.Content = text;
        AutomationProperties.SetName(button, text);
    }

    private static string Purpose(SetupComponent component) => component.Id switch
    {
        "node" => AgentText.PurposeNode,
        "ffmpeg" => AgentText.PurposeFfmpeg,
        "chromium" => AgentText.PurposeChromium,
        _ => component.Purpose,
    };

    private static string Size(long bytes) =>
        bytes >= Gigabyte
            ? string.Create(CultureInfo.CurrentCulture, $"{bytes / Gigabyte:0.0} GB")
            : string.Create(CultureInfo.CurrentCulture, $"{bytes / (1024d * 1024):0} MB");

    private static string FailureText(ComponentState state) => state.Failure switch
    {
        SetupFailure.NoInternet => AgentText.FailureNoInternet,
        SetupFailure.DownloadFailed => AgentText.FailureDownload,
        SetupFailure.VerifyFailed => AgentText.FailureVerify,
        SetupFailure.DiskFull => AgentText.FailureDisk,
        SetupFailure.ReadyCheckFailed => string.Format(CultureInfo.CurrentCulture, AgentText.FailureReadyCheck, state.Component.Name),
        _ => AgentText.FailureInstall,
    };

    private static string StatusText(ComponentState state) => state.Status switch
    {
        ComponentStatus.Downloading => AgentText.StatusDownloading,
        ComponentStatus.Verifying => AgentText.StatusVerifying,
        ComponentStatus.Installing => AgentText.StatusInstalling,
        ComponentStatus.Installed => AgentText.StatusInstalled,
        ComponentStatus.Skipped => AgentText.StatusSkipped,
        ComponentStatus.Failed => string.Format(CultureInfo.CurrentCulture, AgentText.StatusFailed, FailureText(state)),
        ComponentStatus.NeedsRepair => AgentText.StatusNeedsRepair,
        _ => AgentText.StatusWaiting,
    };

    private async Task Track(Task run)
    {
        Running = run;
        Refresh();
        try
        {
            await run.ConfigureAwait(true);
        }
        finally
        {
            Running = null;
            Refresh();
        }
    }

    private void AddRow(ComponentState state)
    {
        var component = state.Component;
        var name = new TextBlock { Text = component.Name, FontWeight = Avalonia.Media.FontWeight.SemiBold };
        var purpose = new TextBlock { Text = Purpose(component), TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        var detail = new TextBlock
        {
            Text = string.Format(CultureInfo.CurrentCulture, AgentText.SetupRowDetail, component.License, Size(component.DownloadBytes), component.Version)
                + (component.Required ? " · " + AgentText.SetupRowRequired : string.Empty),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var status = new TextBlock { FontWeight = Avalonia.Media.FontWeight.SemiBold };
        var retry = new Button { IsVisible = false };
        var report = new Button { IsVisible = false };
        var repair = new Button { IsVisible = false };
        Label(retry, AgentText.SetupRetry);
        Label(report, AgentText.SetupReport);
        Label(repair, AgentText.SetupRepair);
        retry.Click += (_, _) => _ = RetryAsync(component.Id);
        repair.Click += (_, _) => _ = RetryAsync(component.Id);
        report.Click += (_, _) => ReportFailure(component.Id);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { retry, report, repair } };
        var row = new Border
        {
            Padding = new Avalonia.Thickness(10),
            CornerRadius = new Avalonia.CornerRadius(6),
            BorderThickness = new Avalonia.Thickness(1),
            Child = new StackPanel { Spacing = 2, Children = { name, purpose, detail, status, buttons } },
        };
        row.Bind(Border.BackgroundProperty, row.GetResourceObservable("KonseptSurfaceBrush"));
        row.Bind(Border.BorderBrushProperty, row.GetResourceObservable("KonseptBorderBrush"));
        _rows[component.Id] = (row, status, retry, report, repair);
        ComponentList.Children.Add(row);
        ShowRow(state);
    }

    private void ShowRow(ComponentState state)
    {
        if (!_rows.TryGetValue(state.Component.Id, out var row))
        {
            return;
        }

        var text = StatusText(state);
        row.Status.Text = text;
        var failed = state.Status == ComponentStatus.Failed;
        row.Row.Classes.Set("error", failed);
        row.Status.Bind(TextBlock.ForegroundProperty, row.Status.GetResourceObservable(failed ? "KonseptErrorBrush" : "KonseptWindowForeground"));
        row.Row.Bind(Border.BorderBrushProperty, row.Row.GetResourceObservable(failed ? "KonseptErrorBrush" : "KonseptBorderBrush"));
        AutomationProperties.SetName(row.Row, $"{state.Component.Name}: {text}");
        var busy = _setup?.IsRunning == true;
        row.Retry.IsVisible = failed;
        row.Retry.IsEnabled = !busy;
        row.Report.IsVisible = failed && _reports is not null;
        row.Repair.IsVisible = state.Status == ComponentStatus.NeedsRepair;
        row.Repair.IsEnabled = !busy;
    }

    private void Apply(SetupEvent e)
    {
        if (e.State is { } state)
        {
            ShowRow(state);
        }

        var line = LineFor(e);
        if (line is not null)
        {
            _lines.Add(line);
            LogList.ScrollIntoView(line);
        }

        if (e.Kind == SetupEventKind.Ready)
        {
            DoneText.Text = _updating ? AgentText.UpdateDone : AgentText.SetupDone;
            CurrentText.Text = string.Empty;
            Completed?.Invoke(this, EventArgs.Empty);
        }

        Refresh();
    }

    private string? LineFor(SetupEvent e)
    {
        var name = e.State?.Component.Name ?? string.Empty;
        string F(string format, params object[] values) => string.Format(CultureInfo.CurrentCulture, format, values);
        switch (e.Kind)
        {
            case SetupEventKind.Started:
                DoneText.Text = string.Empty;
                return AgentText.LineStarted;
            case SetupEventKind.Retrying:
                return F(AgentText.LineRetrying, name, e.Attempt, Domain.Rules.SetupRules.AutomaticRetries);
            case SetupEventKind.UsingMirror:
                return F(AgentText.LineMirror, name);
            case SetupEventKind.Checking:
                CurrentText.Text = AgentText.SetupChecking;
                return AgentText.SetupChecking;
            case SetupEventKind.Ready:
                return AgentText.SetupDone;
            case SetupEventKind.Stopped:
                CurrentText.Text = string.Empty;
                return AgentText.SetupStopped;
            case SetupEventKind.StatusChanged when e.State is { } state:
                if (state.Status is ComponentStatus.Downloading or ComponentStatus.Verifying or ComponentStatus.Installing)
                {
                    CurrentText.Text = F(AgentText.SetupCurrent, name);
                }

                return state.Status switch
                {
                    ComponentStatus.Downloading => F(AgentText.LineDownloading, name),
                    ComponentStatus.Verifying => F(AgentText.LineVerifying, name),
                    ComponentStatus.Installing => F(AgentText.LineInstalling, name),
                    ComponentStatus.Installed => F(AgentText.LineInstalled, name),
                    ComponentStatus.Failed => F(AgentText.LineFailed, name, FailureText(state)),
                    _ => null,
                };
            default:
                return null;
        }
    }

    private void Refresh()
    {
        if (_setup is null)
        {
            return;
        }

        Progress.Value = _setup.Percent;
        AutomationProperties.SetHelpText(Progress, string.Create(CultureInfo.CurrentCulture, $"%{_setup.Percent}"));
        var running = _setup.IsRunning || Running is not null;
        var pending = _setup.Components.Any(c => c.Status is ComponentStatus.Waiting);
        var preflight = _setup.Preflight();
        SpaceText.Text = string.Format(
            CultureInfo.CurrentCulture,
            AgentText.SetupSpace,
            (preflight.RequiredBytes / Gigabyte).ToString("0.0", CultureInfo.CurrentCulture),
            (preflight.FreeBytes / Gigabyte).ToString("0.0", CultureInfo.CurrentCulture));
        ReasonText.Text = !preflight.Online ? AgentText.SetupReasonOffline
            : !preflight.EnoughSpace ? AgentText.SetupReasonDisk
            : AcceptBox.IsChecked != true && pending ? AgentText.SetupReasonLicense
            : string.Empty;
        StartButton.IsEnabled = !running && pending && AcceptBox.IsChecked == true && preflight.Passed;
        AcceptBox.IsEnabled = !running;
        VideoFolderBox.IsEnabled = !running;
        VideoChangeButton.IsEnabled = !running;
        foreach (var state in _setup.Components)
        {
            ShowRow(state);
        }
    }

    private async Task ChangeVideoFolderAsync()
    {
        if (_pickFolder is null)
        {
            return;
        }

        var folder = await _pickFolder().ConfigureAwait(true);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            SetVideoFolder(folder);
        }
    }

    private async Task<string?> PickFolderWithDialogAsync()
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = AgentText.SetupVideoFolder,
            AllowMultiple = false,
        }).ConfigureAwait(true);
        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }
}
