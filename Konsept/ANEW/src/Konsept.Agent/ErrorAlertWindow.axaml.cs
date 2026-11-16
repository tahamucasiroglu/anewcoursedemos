using System.Globalization;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Domain.Rules;
using Konsept.Agent.Resources;

namespace Konsept.Agent;

/// <summary>
/// Ajan hata uyarısı penceresi (spec 0022 Req 4–8, 15): maskelenmiş hata özeti; "Ayrıntıları göster" gönderilecek metnin tamamını gösterir;
/// "Bildir" gönderir (ya da "Bağlantı kurulunca gönderilecek" ile bekletir); "Kapat" hiçbir şey göndermez; "Hata raporunu dosyaya kaydet"
/// aynı metni kullanıcının seçtiği dosyaya yazar. Düğmeler klavyeyle erişilebilir ve erişilebilir ada sahiptir. Metinler düz metindir.
/// </summary>
public sealed partial class ErrorAlertWindow : Window
{
    private IErrorReportCenter? _center;
    private AgentErrorReport? _report;
    private Func<Task<string?>>? _pickPath;

    /// <summary>Initializes a new instance of the <see cref="ErrorAlertWindow"/> class.</summary>
    public ErrorAlertWindow()
    {
        InitializeComponent();
        Title = AgentText.AlertTitle;
        TitleText.Text = AgentText.AlertTitle;
        IntroText.Text = AgentText.AlertIntro;
        Label(ReportButton, AgentText.AlertReport);
        Label(SaveButton, AgentText.AlertSaveFile);
        Label(CloseButton, AgentText.AlertClose);
        Label(DetailsButton, AgentText.AlertShowDetails);
        ReportButton.Click += (_, _) => _ = ReportAsync();
        SaveButton.Click += (_, _) => _ = SaveAsync();
        CloseButton.Click += (_, _) => Close();
        DetailsButton.Click += (_, _) => ToggleDetails();
    }

    /// <summary>Gets gösterilen özet (testler okur).</summary>
    internal string SummaryDisplay => SummaryText.Text ?? string.Empty;

    /// <summary>Gets ayrıntı metni; kapalıysa boş (testler okur).</summary>
    internal string DetailsDisplay => DetailsPanel.IsVisible ? DetailsText.Text ?? string.Empty : string.Empty;

    /// <summary>Gets durum metni (testler okur).</summary>
    internal string StatusDisplay => StatusText.Text ?? string.Empty;

    /// <summary>Gets düğmelerin erişilebilir adları (testler okur).</summary>
    internal IReadOnlyList<string> ButtonNames =>
        [.. new[] { ReportButton, SaveButton, CloseButton, DetailsButton }.Select(b => AutomationProperties.GetName(b) ?? string.Empty)];

    /// <summary>Gets a value indicating whether "Bildir" kullanılabilir.</summary>
    internal bool ReportEnabled => ReportButton.IsEnabled;

    /// <summary>Pencereyi rapora bağlar.</summary>
    /// <param name="center">Hata bildirimi merkezi.</param>
    /// <param name="report">Rapor.</param>
    /// <param name="pickPath">Dosya seçici (testte sahte); verilmezse sistemin kaydetme penceresi.</param>
    internal void Attach(IErrorReportCenter center, AgentErrorReport report, Func<Task<string?>>? pickPath = null)
    {
        _center = center ?? throw new ArgumentNullException(nameof(center));
        _report = report ?? throw new ArgumentNullException(nameof(report));
        _pickPath = pickPath ?? PickWithDialogAsync;
        SummaryText.Text = report.Summary;
        DetailsText.Text = AgentReportText.Compose(report);
    }

    /// <summary>"Ayrıntıları göster" / "Ayrıntıları gizle".</summary>
    internal void ToggleDetails()
    {
        DetailsPanel.IsVisible = !DetailsPanel.IsVisible;
        Label(DetailsButton, DetailsPanel.IsVisible ? AgentText.AlertHideDetails : AgentText.AlertShowDetails);
    }

    /// <summary>"Bildir": gönderir ya da bekletir; sonucu gösterir.</summary>
    /// <returns>Görev.</returns>
    internal async Task ReportAsync()
    {
        if (_center is null || _report is null)
        {
            return;
        }

        ReportButton.IsEnabled = false;
        StatusText.Text = AgentText.AlertSending;
        var outcome = await _center.ReportAsync(_report, CancellationToken.None).ConfigureAwait(true);
        StatusText.Text = OutcomeLabel(outcome);
    }

    /// <summary>"Hata raporunu dosyaya kaydet".</summary>
    /// <returns>Görev.</returns>
    internal async Task SaveAsync()
    {
        if (_center is null || _report is null || _pickPath is null)
        {
            return;
        }

        var path = await _pickPath().ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            await _center.SaveToFileAsync(_report, path, CancellationToken.None).ConfigureAwait(true);
            StatusText.Text = string.Format(CultureInfo.CurrentCulture, AgentText.AlertSaved, path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusText.Text = AgentText.AlertSaveFailed;
        }
    }

    /// <summary>Sonucun kullanıcıya gösterilen metni.</summary>
    /// <param name="outcome">Sonuç.</param>
    /// <returns>Metin.</returns>
    internal static string OutcomeLabel(ReportOutcome outcome) => outcome switch
    {
        ReportOutcome.Sent => AgentText.AlertSent,
        ReportOutcome.Queued => AgentText.AlertQueued,
        ReportOutcome.OtherAccount => AgentText.AlertOtherAccount,
        ReportOutcome.RateLimited => AgentText.AlertRateLimited,
        _ => AgentText.AlertRejected,
    };

    private static void Label(Button button, string text)
    {
        button.Content = text;
        AutomationProperties.SetName(button, text);
    }

    private async Task<string?> PickWithDialogAsync()
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = AgentText.AlertSaveFile,
            SuggestedFileName = AgentText.AlertFileName,
            DefaultExtension = "txt",
        }).ConfigureAwait(true);
        return file?.TryGetLocalPath();
    }
}
