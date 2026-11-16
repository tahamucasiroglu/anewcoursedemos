using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;

namespace Konsept.Agent.Application.Base;

/// <summary>
/// Oturum akışının mantığı (spec 0012 Req 1, 4–6, 9, 11–12): saklı belirteç varsa doğrudan bağlanır (yeniden eşleştirme yok);
/// yoksa kod alır, süresi dolana ya da karar gelene kadar sorar; onayda belirteci saklayıp bağlanır. Sunucu oturumu kapatırsa
/// (çıkış, yeni eşleştirme, süresi dolmuş/iptal belirteç) bağlantı askıya alınır, belirteç silinir, "Oturum kapandı" ile yeni
/// kod gösterilir. Sunucuya ulaşılamazken "Sunucuya ulaşılamıyor" gösterilir ve yeniden denenir. Kod, sır ve belirteç
/// loglanmaz. Bekleme ve saat kaynağı alt sınıftadır.
/// </summary>
public abstract class AgentSessionCoordinatorBase : IAgentSessionCoordinator
{
    private readonly Lock _gate = new();
    private readonly IServerConnection _connection;
    private readonly IPairingClient _pairing;
    private readonly IAgentTokenStore _tokens;
    private readonly IAgentInfoProvider _info;
    private readonly AgentSessionOptions _options;
    private CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _pairingRun;
    private AgentSessionSnapshot _current = new(AgentSessionState.Unpaired);
    private bool _paired;
    private bool _closedNotice;

    /// <summary>Initializes a new instance of the <see cref="AgentSessionCoordinatorBase"/> class.</summary>
    /// <param name="connection">Sunucu bağlantısı.</param>
    /// <param name="pairing">Eşleştirme uçları.</param>
    /// <param name="tokens">Belirteç deposu.</param>
    /// <param name="info">Ajan bilgisi.</param>
    /// <param name="options">Zamanlamalar.</param>
    protected AgentSessionCoordinatorBase(IServerConnection connection, IPairingClient pairing, IAgentTokenStore tokens, IAgentInfoProvider info, AgentSessionOptions options)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _pairing = pairing ?? throw new ArgumentNullException(nameof(pairing));
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        _info = info ?? throw new ArgumentNullException(nameof(info));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _connection.StateChanged += (_, state) => OnConnectionStateChanged(state);
        _connection.SessionRevoked += (_, _) => _ = OnSessionRevokedAsync();
    }

    /// <inheritdoc />
    public event EventHandler<AgentSessionSnapshot>? Changed;

    /// <inheritdoc />
    public AgentSessionSnapshot Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_tokens.Read()))
        {
            return BeginPairedAsync();
        }

        StartPairing();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void RequestNewCode()
    {
        lock (_gate)
        {
            if (_paired || _current.State is AgentSessionState.Unreachable)
            {
                return;
            }
        }

        StartPairing();
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        CancellationTokenSource lifetime;
        lock (_gate)
        {
            lifetime = _lifetime;
            _pairingRun?.Cancel();
        }

        await lifetime.CancelAsync().ConfigureAwait(false);
    }

    /// <summary>Bekler.</summary>
    /// <param name="delay">Süre.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected abstract Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);

    /// <summary>Şimdiki zaman (UTC).</summary>
    /// <returns>Zaman.</returns>
    protected abstract DateTimeOffset UtcNow();

    private static bool IsUnreachable(Exception ex) => ex is HttpRequestException or TaskCanceledException or TimeoutException;

    private async Task BeginPairedAsync()
    {
        lock (_gate)
        {
            _paired = true;
            _closedNotice = false;
        }

        Publish(new AgentSessionSnapshot(_connection.State == ConnectionState.Connected ? AgentSessionState.Paired : AgentSessionState.Unreachable));
        await _connection.StartAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private void OnConnectionStateChanged(ConnectionState state)
    {
        lock (_gate)
        {
            if (!_paired)
            {
                return;
            }
        }

        Publish(new AgentSessionSnapshot(state == ConnectionState.Connected ? AgentSessionState.Paired : AgentSessionState.Unreachable));
    }

    private async Task OnSessionRevokedAsync()
    {
        lock (_gate)
        {
            if (!_paired)
            {
                return;
            }

            _paired = false;
            _closedNotice = true;
        }

        _tokens.Clear();
        Publish(new AgentSessionSnapshot(AgentSessionState.SessionClosed));
        try
        {
            await _connection.SuspendAsync(CancellationToken.None).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Bağlantı zaten kapanmış olabilir; akış yeni kodla sürer.
        catch (Exception)
#pragma warning restore CA1031
        {
        }

        StartPairing();
    }

    private void StartPairing()
    {
        CancellationToken token;
        lock (_gate)
        {
            _pairingRun?.Cancel();
            _pairingRun?.Dispose();
            _pairingRun = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            token = _pairingRun.Token;
        }

        _ = Task.Run(() => PairingLoopAsync(token), CancellationToken.None);
    }

    private async Task PairingLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                PairingCodeInfo code;
                try
                {
                    code = await _pairing.RequestCodeAsync(_info.MachineName, _info.Version, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (IsUnreachable(ex) && !cancellationToken.IsCancellationRequested)
                {
                    Publish(new AgentSessionSnapshot(AgentSessionState.Unreachable));
                    await DelayAsync(_options.RetryDelay, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                bool closed;
                lock (_gate)
                {
                    closed = _closedNotice;
                }

                Publish(new AgentSessionSnapshot(closed ? AgentSessionState.SessionClosed : AgentSessionState.Unpaired, code.Code, code.ExpiresAt));
                if (await WaitForDecisionAsync(code, cancellationToken).ConfigureAwait(false))
                {
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Yeni kod istendi ya da ajan kapanıyor.
        }
    }

    // true: akış bitti (eşleşti, reddedildi ya da süresi doldu — kullanıcı yeni kod ister); false: yeni kod alınmalı.
    private async Task<bool> WaitForDecisionAsync(PairingCodeInfo code, CancellationToken cancellationToken)
    {
        while (true)
        {
            await DelayAsync(_options.PollInterval, cancellationToken).ConfigureAwait(false);
            if (UtcNow() >= code.ExpiresAt)
            {
                Publish(new AgentSessionSnapshot(AgentSessionState.CodeExpired));
                return true;
            }

            PairingPoll poll;
            try
            {
                poll = await _pairing.CheckAsync(code.Secret, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsUnreachable(ex) && !cancellationToken.IsCancellationRequested)
            {
                continue; // Kod geçerliyken kısa kopukluk: kodu göstermeye devam et.
            }

            switch (poll.State)
            {
                case PairingPollState.Approved when !string.IsNullOrEmpty(poll.Token):
                    _tokens.Save(poll.Token);
                    await BeginPairedAsync().ConfigureAwait(false);
                    return true;
                case PairingPollState.Rejected:
                    Publish(new AgentSessionSnapshot(AgentSessionState.Rejected));
                    return true;
                case PairingPollState.Expired:
                    Publish(new AgentSessionSnapshot(AgentSessionState.CodeExpired));
                    return true;
                default:
                    continue;
            }
        }
    }

    private void Publish(AgentSessionSnapshot snapshot)
    {
        lock (_gate)
        {
            if (_current == snapshot)
            {
                return;
            }

            _current = snapshot;
        }

        Changed?.Invoke(this, snapshot);
    }
}
