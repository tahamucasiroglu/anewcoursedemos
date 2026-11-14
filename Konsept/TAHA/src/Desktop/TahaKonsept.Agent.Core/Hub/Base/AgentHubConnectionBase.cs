using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Core.Auth.Abstract;
using TahaKonsept.Agent.Core.Configuration;
using TahaKonsept.Agent.Core.Configuration.Abstract;
using TahaKonsept.Agent.Core.Hub.Abstract;
using TahaKonsept.Agent.Core.Hub.Concrete;
using TahaKonsept.Shared.Contracts.Agent;
using TahaKonsept.Shared.Contracts.Common;

namespace TahaKonsept.Agent.Core.Hub.Base;

/// <summary>
/// SignalR hub bağlantı döngüsü. Eşleme yoksa <see cref="HubConnectionStatus.Unauthorized"/> durumunda bekler; eşlenince bağlanır,
/// <c>AccessTokenProvider</c> = <see cref="IAgentAuthService.GetAccessTokenAsync"/>, otomatik yeniden bağlanma (<see cref="AgentReconnectPolicy"/>),
/// her (yeniden) bağlantıda <c>Hello</c> + bekleyen raporların gönderimi, periyodik <c>Heartbeat</c>.
/// 401'de token yenilenir; yenileme reddedilirse oturum kapanır ve döngü eşleme bekler. Sunucu adresi değişince yeniden bağlanır.
/// Sunucu komutları <see cref="ResolveClient"/>'ın döndürdüğü <see cref="IAgentClient"/>'a iletilir.
/// </summary>
public abstract class AgentHubConnectionBase : IAgentHubConnection, IAsyncDisposable
{
    private readonly IAgentAuthService _auth;
    private readonly IAgentSettingsStore _settings;
    private readonly IAgentHelloBuilder _hello;
    private readonly AgentOptions _options;
    private readonly ConcurrentQueue<PendingHubMessage> _pending = new();
    private readonly object _gate = new();
    private TaskCompletionSource _wake = NewSignal();
    private CancellationTokenSource? _loopCts;
    private Task? _loopTask;
    private HubConnection? _connection;
    private string? _connectedBaseUrl;
    private HubConnectionStatus _status = HubConnectionStatus.Disconnected;

    /// <summary>Oluşturur.</summary>
    /// <param name="auth">Oturum servisi.</param>
    /// <param name="settings">Ayar deposu.</param>
    /// <param name="hello">Hello üretici.</param>
    /// <param name="options">Seçenekler.</param>
    /// <param name="logger">Logger.</param>
    protected AgentHubConnectionBase(IAgentAuthService auth, IAgentSettingsStore settings, IAgentHelloBuilder hello, AgentOptions options, ILogger logger)
    {
        _auth = auth;
        _settings = settings;
        _hello = hello;
        _options = options;
        Logger = logger;
    }

    /// <inheritdoc />
    public event EventHandler<HubConnectionStatus>? StatusChanged;

    /// <summary>Logger.</summary>
    protected ILogger Logger { get; }

    /// <inheritdoc />
    public HubConnectionStatus Status => _status;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken ct)
    {
        lock (_gate)
        {
            if (_loopTask is { IsCompleted: false })
            {
                return Task.CompletedTask;
            }

            _auth.AuthorizationChanged += OnWakeEvent;
            _settings.Changed += OnSettingsChanged;
            _loopCts = new CancellationTokenSource();
            var token = _loopCts.Token;
            _loopTask = Task.Run(() => RunLoopAsync(token), CancellationToken.None);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken ct)
    {
        Task? loop;
        lock (_gate)
        {
            _auth.AuthorizationChanged -= OnWakeEvent;
            _settings.Changed -= OnSettingsChanged;
            _loopCts?.Cancel();
            loop = _loopTask;
        }

        if (loop is not null)
        {
            try
            {
                await loop.WaitAsync(TimeSpan.FromSeconds(10), ct);
            }
            catch (TimeoutException)
            {
                Logger.LogWarning("Hub bağlantı döngüsü zamanında durmadı.");
            }
        }

        await DisposeConnectionAsync();
        lock (_gate)
        {
            _loopCts?.Dispose();
            _loopCts = null;
            _loopTask = null;
        }

        SetStatus(HubConnectionStatus.Disconnected);
    }

    /// <inheritdoc />
    public async Task SendHelloAsync(CancellationToken ct)
    {
        var connection = _connection;
        if (connection is not { State: HubConnectionState.Connected })
        {
            return;
        }

        var hello = await _hello.BuildAsync(ct);
        await connection.InvokeAsync(nameof(IAgentHub.Hello), hello, ct);
    }

    /// <inheritdoc />
    public Task ReportProgressAsync(StageProgressReport report, CancellationToken ct) =>
        SendAsync(nameof(IAgentHub.StageProgress), report, queueIfOffline: false, ct);

    /// <inheritdoc />
    public Task ReportCompletedAsync(StageResultReport report, CancellationToken ct) =>
        SendAsync(nameof(IAgentHub.StageCompleted), report, queueIfOffline: true, ct);

    /// <inheritdoc />
    public Task ReportFailedAsync(StageFailureReport report, CancellationToken ct) =>
        SendAsync(nameof(IAgentHub.StageFailed), report, queueIfOffline: true, ct);

    /// <inheritdoc />
    public Task LogAsync(AgentLogEntry entry, CancellationToken ct) =>
        SendAsync(nameof(IAgentHub.Log), entry, queueIfOffline: false, ct);

    /// <inheritdoc />
    public Task ReportSpeechPreviewAsync(SpeechPreviewResult result, CancellationToken ct) =>
        SendAsync(nameof(IAgentHub.SpeechPreviewCompleted), result, queueIfOffline: false, ct);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        GC.SuppressFinalize(this);
    }

    /// <summary>Sunucu komutlarını işleyecek istemciyi döner (her çağrıda; döngüsel bağımlılığı önlemek için geç çözülür).</summary>
    /// <returns>Komut işleyici.</returns>
    protected abstract IAgentClient ResolveClient();

    /// <summary>Hub bağlantısını kurar (testlerde değiştirilebilir).</summary>
    /// <param name="hubUrl">Hub adresi.</param>
    /// <returns>Bağlantı.</returns>
    protected virtual HubConnection CreateConnection(Uri hubUrl) =>
        new HubConnectionBuilder()
            .WithUrl(hubUrl, options => options.AccessTokenProvider = () => _auth.GetAccessTokenAsync(CancellationToken.None))
            .WithAutomaticReconnect(new AgentReconnectPolicy(OnReconnectAttempt))
            .AddJsonProtocol(options => ContractJson.Apply(options.PayloadSerializerOptions))
            .Build();

    private async Task RunLoopAsync(CancellationToken ct)
    {
        long attempt = 0;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (!_auth.IsPaired)
                {
                    SetStatus(HubConnectionStatus.Unauthorized);
                    await WaitForWakeAsync(TimeSpan.FromSeconds(30), ct);
                    continue;
                }

                await ConnectAndServeAsync(ct);
                attempt = 0;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                Logger.LogWarning("Hub bağlantısı yetkisiz ({Status}); token yenileniyor.", ex.StatusCode);
                await _auth.RefreshAsync(ct);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Hub'a bağlanılamadı: {Message}", ex.Message);
            }
            finally
            {
                await DisposeConnectionAsync();
            }

            if (ct.IsCancellationRequested)
            {
                break;
            }

            SetStatus(_auth.IsPaired ? HubConnectionStatus.Reconnecting : HubConnectionStatus.Unauthorized);
            await WaitForWakeAsync(AgentReconnectPolicy.GetDelay(Math.Max(1, attempt++)), ct);
        }

        SetStatus(HubConnectionStatus.Disconnected);
    }

    private async Task ConnectAndServeAsync(CancellationToken ct)
    {
        var baseUrl = _settings.Current.ServerBaseUrl.TrimEnd('/');
        var hubUrl = new Uri(baseUrl + AgentHubRoutes.HubPath, UriKind.Absolute);
        SetStatus(HubConnectionStatus.Connecting);

        var connection = CreateConnection(hubUrl);
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.Closed += ex =>
        {
            closed.TrySetResult();
            return Task.CompletedTask;
        };
        connection.Reconnecting += ex =>
        {
            SetStatus(HubConnectionStatus.Reconnecting);
            Logger.LogWarning("Hub bağlantısı koptu, yeniden bağlanılıyor: {Message}", ex?.Message);
            return Task.CompletedTask;
        };
        connection.Reconnected += _ =>
        {
            SetStatus(HubConnectionStatus.Connected);
            return OnConnectedAsync(ct);
        };
        RegisterHandlers(connection);

        _connection = connection;
        _connectedBaseUrl = baseUrl;
        await connection.StartAsync(ct);
        SetStatus(HubConnectionStatus.Connected);
        Logger.LogInformation("Hub'a bağlanıldı: {Url}", hubUrl);
        try
        {
            await OnConnectedAsync(ct);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested && closed.Task.IsCompleted)
        {
            // Sunucu bağlantıyı tanıtım bitmeden kesti: cihaz iptal edilmiş olabilir (ör. web'den tüm oturumları kapat).
            // Erişim token'ı henüz geçerli olduğundan 401 gelmez; yenileme reddedilirse oturum kapanır ve yeniden eşleme istenir.
            await HandleRejectedConnectionAsync(ct);
            return;
        }

        if (closed.Task.IsCompleted)
        {
            await HandleRejectedConnectionAsync(ct);
            return;
        }

        await ServeAsync(connection, closed.Task, ct);
    }

    /// <summary>Bağlantı açılır açılmaz sunucu tarafından kapatıldığında çağrılır: token'ı yenilemeyi dener.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    private async Task HandleRejectedConnectionAsync(CancellationToken ct)
    {
        Logger.LogWarning("Sunucu hub bağlantısını hemen kapattı (cihaz yetkisi kaldırılmış olabilir); token yenileniyor.");
        var refreshed = await _auth.RefreshAsync(ct);
        if (!refreshed && !_auth.IsPaired)
        {
            Logger.LogWarning("Sunucu yetkiyi kaldırdı; ajan yeniden eşlenmeli.");
        }
    }

    private async Task ServeAsync(HubConnection connection, Task closed, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(_options.HeartbeatInterval);
        var wake = Volatile.Read(ref _wake).Task;
        var tick = timer.WaitForNextTickAsync(ct).AsTask();
        while (true)
        {
            var done = await Task.WhenAny(closed, wake, tick);
            ct.ThrowIfCancellationRequested();
            if (done == closed)
            {
                return;
            }

            if (done == wake)
            {
                if (!_auth.IsPaired || !string.Equals(_connectedBaseUrl, _settings.Current.ServerBaseUrl.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                wake = Volatile.Read(ref _wake).Task;
                continue;
            }

            tick = timer.WaitForNextTickAsync(ct).AsTask();
            if (connection.State == HubConnectionState.Connected)
            {
                await TrySendAsync(connection, nameof(IAgentHub.Heartbeat), null, ct);
            }
        }
    }

    private async Task OnConnectedAsync(CancellationToken ct)
    {
        try
        {
            await SendHelloAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogWarning(ex, "Hello gönderilemedi.");
        }

        var connection = _connection;
        while (connection is { State: HubConnectionState.Connected } && _pending.TryPeek(out var message))
        {
            if (!await TrySendAsync(connection, message.Method, message.Argument, ct))
            {
                break;
            }

            _pending.TryDequeue(out _);
        }
    }

    private void RegisterHandlers(HubConnection connection)
    {
        connection.On<StageCommand>(nameof(IAgentClient.ExecuteStage), command => ResolveClient().ExecuteStage(command));
        connection.On<Guid>(nameof(IAgentClient.CancelJob), jobId => ResolveClient().CancelJob(jobId));
        connection.On<Guid>(nameof(IAgentClient.PauseJob), jobId => ResolveClient().PauseJob(jobId));
        connection.On<Guid>(nameof(IAgentClient.OpenFolder), videoId => ResolveClient().OpenFolder(videoId));
        connection.On<Guid>(nameof(IAgentClient.OpenInSystemPlayer), videoId => ResolveClient().OpenInSystemPlayer(videoId));
        connection.On<Guid>(nameof(IAgentClient.DeleteVideo), videoId => ResolveClient().DeleteVideo(videoId));
        connection.On<SpeechPreviewCommand>(nameof(IAgentClient.PreviewSpeech), command => ResolveClient().PreviewSpeech(command));
        connection.On(nameof(IAgentClient.RefreshCapabilities), () => ResolveClient().RefreshCapabilities());
    }

    private async Task SendAsync(string method, object argument, bool queueIfOffline, CancellationToken ct)
    {
        var connection = _connection;
        if (connection is { State: HubConnectionState.Connected } && await TrySendAsync(connection, method, argument, ct))
        {
            return;
        }

        if (queueIfOffline)
        {
            _pending.Enqueue(new PendingHubMessage(method, argument));
            Logger.LogInformation("{Method} raporu bağlantı gelince gönderilmek üzere kuyruğa alındı.", method);
        }
    }

    private async Task<bool> TrySendAsync(HubConnection connection, string method, object? argument, CancellationToken ct)
    {
        try
        {
            if (argument is null)
            {
                await connection.InvokeAsync(method, ct);
            }
            else
            {
                await connection.InvokeAsync(method, argument, ct);
            }

            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            Logger.LogWarning("Hub çağrısı başarısız ({Method}): {Message}", method, ex.Message);
            return false;
        }
    }

    private void OnReconnectAttempt(Exception? reason)
    {
        if (reason is HttpRequestException { StatusCode: HttpStatusCode.Unauthorized })
        {
            _ = _auth.RefreshAsync(CancellationToken.None);
        }
    }

    private async Task DisposeConnectionAsync()
    {
        var connection = Interlocked.Exchange(ref _connection, null);
        if (connection is null)
        {
            return;
        }

        try
        {
            await connection.DisposeAsync();
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Hub bağlantısı kapatılırken hata.");
        }
    }

    private async Task WaitForWakeAsync(TimeSpan timeout, CancellationToken ct)
    {
        var wake = Volatile.Read(ref _wake).Task;
        await Task.WhenAny(wake, Task.Delay(timeout, ct));
        ct.ThrowIfCancellationRequested();
    }

    private void OnWakeEvent(object? sender, EventArgs e) => Interlocked.Exchange(ref _wake, NewSignal()).TrySetResult();

    private void OnSettingsChanged(object? sender, AgentSettings e) => OnWakeEvent(sender, EventArgs.Empty);

    private void SetStatus(HubConnectionStatus status)
    {
        if (_status == status)
        {
            return;
        }

        _status = status;
        StatusChanged?.Invoke(this, status);
    }

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
