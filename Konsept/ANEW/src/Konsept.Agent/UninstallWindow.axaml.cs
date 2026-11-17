using System.Globalization;
using Avalonia.Automation;
using Avalonia.Controls;
using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Resources;

namespace Konsept.Agent;

/// <summary>
/// Kaldırma penceresi (spec 0023 Req 11, AC-11, AC-14): "Videolarınız da silinsin mi?" sorusunun varsayılanı <b>Hayır</b>dır (odak ve Enter
/// Hayır'da; pencere kapatılırsa hiçbir şey silinmez). Cevaptan sonra ajan kapatılır, kurulum yeri ve kayıtlar silinir; sonuç metinle gösterilir.
/// </summary>
public sealed partial class UninstallWindow : Window
{
    private IAgentInstallation? _installation;
    private string? _videoFolder;

    /// <summary>Initializes a new instance of the <see cref="UninstallWindow"/> class.</summary>
    public UninstallWindow()
    {
        InitializeComponent();
        Title = AgentText.UninstallTitle;
        HeadingText.Text = AgentText.UninstallHeading;
        DetailText.Text = AgentText.UninstallDetail;
        QuestionText.Text = AgentText.UninstallQuestion;
        Label(NoButton, AgentText.UninstallNo);
        Label(YesButton, AgentText.UninstallYes);
        Label(CloseButton, AgentText.UpdateClose);
        NoButton.Click += (_, _) => _ = AnswerAsync(false);
        YesButton.Click += (_, _) => _ = AnswerAsync(true);
        CloseButton.Click += (_, _) => Close();
        Opened += (_, _) => NoButton.Focus();
    }

    /// <summary>Gets a value indicating whether kaldırma bitti.</summary>
    internal bool Finished { get; private set; }

    /// <summary>Gets durum metni.</summary>
    internal string Status => StatusText.Text ?? string.Empty;

    /// <summary>Gets varsayılan (Enter) düğmenin metni.</summary>
    internal string DefaultAnswer => (string)(IsDefault(NoButton) ? NoButton.Content! : YesButton.Content!);

    /// <summary>Gets tüm etkileşimli kontrollerin erişilebilir adları.</summary>
    internal IReadOnlyList<string> AccessibleNames =>
        [.. new Control[] { NoButton, YesButton, CloseButton }.Select(c => AutomationProperties.GetName(c) ?? string.Empty)];

    /// <summary>Pencereyi kurulu kopyaya bağlar.</summary>
    /// <param name="installation">Kurulu kopya.</param>
    /// <param name="videoFolder">Video klasörü.</param>
    internal void Attach(IAgentInstallation installation, string? videoFolder)
    {
        _installation = installation ?? throw new ArgumentNullException(nameof(installation));
        _videoFolder = videoFolder;
        VideoFolderText.Text = string.Format(CultureInfo.CurrentCulture, AgentText.UninstallVideoFolder, videoFolder);
    }

    /// <summary>Soruyu cevaplar ve kaldırır.</summary>
    /// <param name="deleteVideos">Evet → videolar da silinir.</param>
    /// <returns>Görev.</returns>
    internal async Task AnswerAsync(bool deleteVideos)
    {
        if (_installation is null || !AnswerPanel.IsVisible)
        {
            return;
        }

        AnswerPanel.IsVisible = false;
        StatusText.Text = AgentText.UninstallRunning;
        var installation = _installation;
        var folder = _videoFolder;
        var ok = await Task.Run(() => installation.Uninstall(deleteVideos, folder)).ConfigureAwait(true);
        StatusText.Text = ok ? AgentText.UninstallDone : AgentText.UninstallPartial;
        Finished = true;
        CloseButton.IsVisible = true;
        CloseButton.Focus();
    }

    private static bool IsDefault(Button button) => button.IsDefault;

    private static void Label(Button button, string text)
    {
        button.Content = text;
        AutomationProperties.SetName(button, text);
    }
}
