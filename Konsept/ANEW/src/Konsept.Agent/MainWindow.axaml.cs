using Avalonia.Controls;
using Avalonia.Threading;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Resources;

namespace Konsept.Agent;

/// <summary>
/// Ajanın durum penceresi: bağlantı durumu, sunucu ve sürüm. Kapatmak uygulamayı kapatmaz, pencereyi gizler
/// (spec 0001 Req 6, AC-5).
/// </summary>
public sealed partial class MainWindow : Window
{
    /// <summary>Initializes a new instance of the <see cref="MainWindow"/> class.</summary>
    public MainWindow()
    {
        InitializeComponent();
        Title = AgentText.AppTitle;
        HintText.Text = AgentText.CloseHint;
        ShowState(ConnectionState.Disconnected);
    }

    /// <summary>Gets durum metni (testler okur).</summary>
    internal string StateDisplay => StateText.Text ?? string.Empty;

    /// <summary>Pencereyi bileşime bağlar: sunucu, sürüm ve canlı bağlantı durumu.</summary>
    /// <param name="composition">Ajan bileşimi.</param>
    internal void Attach(AgentComposition composition)
    {
        ArgumentNullException.ThrowIfNull(composition);
        ServerText.Text = $"{AgentText.ServerLabel}: {composition.Settings.ServerUrl}";
        VersionText.Text = $"{AgentText.VersionLabel}: {composition.Info.Version} · {composition.Info.MachineName}";
        ShowState(composition.Connection.State);
        composition.Connection.StateChanged += (_, state) => Dispatcher.UIThread.Post(() => ShowState(state));
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

    private void ShowState(ConnectionState state) => StateText.Text = StateLabel(state);
}
