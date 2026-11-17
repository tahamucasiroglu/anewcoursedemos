using System.Globalization;
using Avalonia.Controls;
using Avalonia.Threading;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Resources;

namespace Konsept.Agent;

/// <summary>
/// Ajanın durum penceresi: bağlantı durumu, sunucu ve sürüm; eşleştirme (spec 0012): oturum durumu, büyük ve seçilip
/// kopyalanabilir kod, 10 dakikalık geri sayım, "Yeni kod al" ve "Siteye git" (web'deki "Ajanı eşleştir" sayfası; kod adreste
/// taşınmaz). Kapatmak uygulamayı kapatmaz, pencereyi gizler (spec 0001 Req 6, AC-5).
/// </summary>
public sealed partial class MainWindow : Window
{
    /// <summary>Web'deki eşleştirme sayfasının yolu (spec 0012 Req 2).</summary>
    internal const string PairingPagePath = "ajan-eslestir";

    private readonly DispatcherTimer _countdown;
    private AgentComposition? _composition;
    private AgentSessionSnapshot? _session;

    /// <summary>Initializes a new instance of the <see cref="MainWindow"/> class.</summary>
    public MainWindow()
    {
        InitializeComponent();
        Title = AgentText.AppTitle;
        HintText.Text = AgentText.CloseHint;
        NewCodeButton.Content = AgentText.NewCode;
        OpenPairingButton.Content = AgentText.OpenSite;
        NewCodeButton.Click += (_, _) => _composition?.Session?.RequestNewCode();
        OpenPairingButton.Click += (_, _) => OpenPairingPage();
        _countdown = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, (_, _) => ShowCountdown());
        ShowState(ConnectionState.Disconnected);
    }

    /// <summary>Gets durum metni (testler okur).</summary>
    internal string StateDisplay => StateText.Text ?? string.Empty;

    /// <summary>Gets ipucu metni (testler okur; B-153 tarayıcı açılamadı mesajı).</summary>
    internal string HintDisplay => HintText.Text ?? string.Empty;

    /// <summary>Gets oturum metni (testler okur).</summary>
    internal string SessionDisplay => SessionText.Text ?? string.Empty;

    /// <summary>Gets gösterilen kod; görünmüyorsa boş (testler okur).</summary>
    internal string CodeDisplay => CodeText.IsVisible ? CodeText.Text ?? string.Empty : string.Empty;

    /// <summary>Gets geri sayım metni (testler okur).</summary>
    internal string CountdownDisplay => CountdownText.IsVisible ? CountdownText.Text ?? string.Empty : string.Empty;

    /// <summary>Gets kodu gösteren denetim (seçilip kopyalanabilir olduğu testlerde doğrulanır).</summary>
    internal Control CodeControl => CodeText;

    /// <summary>Gets a value indicating whether "Yeni kod al" düğmesi görünür.</summary>
    internal bool NewCodeVisible => NewCodeButton.IsVisible;

    /// <summary>Pencereyi bileşime bağlar: sunucu, sürüm, canlı bağlantı durumu ve (varsa) oturum akışı.</summary>
    /// <param name="composition">Ajan bileşimi.</param>
    internal void Attach(AgentComposition composition)
    {
        ArgumentNullException.ThrowIfNull(composition);
        _composition = composition;
        ServerText.Text = $"{AgentText.ServerLabel}: {composition.Settings.ServerUrl}";
        VersionText.Text = $"{AgentText.VersionLabel}: {composition.Info.Version} · {composition.Info.MachineName}";
        ShowState(composition.Connection.State);
        composition.Connection.StateChanged += (_, state) => Dispatcher.UIThread.Post(() => ShowState(state));
        if (composition.Session is { } session)
        {
            OpenPairingButton.IsVisible = true;
            ShowSession(session.Current);
            session.Changed += (_, snapshot) => Dispatcher.UIThread.Post(() => ShowSession(snapshot));
        }
    }

    /// <summary>"Siteye git": web'deki "Ajanı eşleştir" sayfasını açar; kod adreste taşınmaz (spec 0012 Req 2).</summary>
    internal void OpenPairingPage()
    {
        if (_composition is not null)
        {
            var address = new Uri(_composition.Settings.SiteUrl, PairingPagePath);
            if (!_composition.Browser.Open(address))
            {
                // B-153: tarayıcı açılamadı — ajan düşmez, adres elle açılsın diye gösterilir.
                HintText.Text = string.Format(CultureInfo.CurrentCulture, AgentText.BrowserOpenFailed, address.AbsoluteUri);
            }
        }
    }

    /// <summary>Oturum durumunu gösterir.</summary>
    /// <param name="snapshot">Anlık durum.</param>
    internal void ShowSession(AgentSessionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _session = snapshot;
        SessionText.Text = SessionLabel(snapshot.State);
        var hasCode = !string.IsNullOrEmpty(snapshot.Code);
        CodeText.Text = snapshot.Code ?? string.Empty;
        CodeText.IsVisible = hasCode;
        CountdownText.IsVisible = hasCode && snapshot.ExpiresAt is not null;
        NewCodeButton.IsVisible = snapshot.State is AgentSessionState.CodeExpired or AgentSessionState.Rejected;
        ShowCountdown();
        if (CountdownText.IsVisible)
        {
            _countdown.Start();
        }
        else
        {
            _countdown.Stop();
        }
    }

    /// <inheritdoc />
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (e.CloseReason != WindowCloseReason.ApplicationShutdown)
        {
            e.Cancel = true;
            Hide();
        }

        base.OnClosing(e);
    }

    /// <summary>Bağlantı durumunun kullanıcıya gösterilen metni (pencere ve tray menüsü başlığı ortak kullanır).</summary>
    /// <param name="state">Bağlantı durumu.</param>
    /// <returns>Durum metni.</returns>
    internal static string StateLabel(ConnectionState state) => state switch
    {
        ConnectionState.Connected => AgentText.StateConnected,
        ConnectionState.Connecting => AgentText.StateConnecting,
        _ => AgentText.StateDisconnected,
    };

    /// <summary>Oturum durumunun penceredeki açıklaması.</summary>
    /// <param name="state">Oturum durumu.</param>
    /// <returns>Metin.</returns>
    internal static string SessionLabel(AgentSessionState state) => state switch
    {
        AgentSessionState.Paired => AgentText.SessionPaired,
        AgentSessionState.SessionClosed => AgentText.SessionClosed,
        AgentSessionState.CodeExpired => AgentText.SessionCodeExpired,
        AgentSessionState.Rejected => AgentText.SessionRejected,
        AgentSessionState.Unreachable => AgentText.SessionUnreachable,
        _ => AgentText.SessionUnpaired,
    };

    /// <summary>Tray menüsünün en üst satırındaki kısa oturum durumu (spec 0012 Req 15).</summary>
    /// <param name="state">Oturum durumu.</param>
    /// <returns>"Eşleşmedi" · "Eşleşti" · "Oturum kapandı" · "Sunucuya ulaşılamıyor".</returns>
    internal static string TrayLabel(AgentSessionState state) => state switch
    {
        AgentSessionState.Paired => AgentText.TrayPaired,
        AgentSessionState.SessionClosed => AgentText.TraySessionClosed,
        AgentSessionState.Unreachable => AgentText.TrayUnreachable,
        _ => AgentText.TrayUnpaired,
    };

    private void ShowState(ConnectionState state) => StateText.Text = StateLabel(state);

    private void ShowCountdown()
    {
        if (_session?.ExpiresAt is not { } expiresAt)
        {
            return;
        }

        var left = expiresAt - DateTimeOffset.UtcNow;
        if (left < TimeSpan.Zero)
        {
            left = TimeSpan.Zero;
        }

        CountdownText.Text = string.Format(CultureInfo.CurrentCulture, AgentText.CodeCountdownFormat, left.ToString(@"mm\:ss", CultureInfo.InvariantCulture));
    }
}
