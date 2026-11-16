using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Domain.Rules;

namespace Konsept.Agent.Application.Base;

/// <summary>
/// Ajan hata bildirimi mantığı (spec 0022 Req 4–9, 15). Tekrar denetimi bellektedir (ajan yeniden başlayınca aynı hata yeniden sorulur).
/// Bekleyen bildirim oluştuğu andaki hesaba bağlanır (bilinen hesap kalıcı tutulur; oturum kapanınca silinir); bağlantı kurulunca hesap
/// sunucudan okunur, başka hesaba ait bildirim gönderilmez ve kullanıcıya bildirilir, hesapsız bildirim ilk eşleşen hesapla gider.
/// Kuyruk en fazla 20; fazlası en eskiyi düşürür (Warning). Gönderim tek seferde bir akışla yapılır (tam bir kez). Teknik log ve
/// dosya yazımı alt sınıftadır.
/// </summary>
public abstract class ErrorReportCenterBase : IErrorReportCenter
{
    private readonly Lock _gate = new();
    private readonly SemaphoreSlim _flush = new(1, 1);
    private readonly HashSet<string> _seen = new(StringComparer.Ordinal);
    private readonly IServerConnection _connection;
    private readonly IErrorReportQueue _queue;
    private readonly IRecentLogSource _logs;
    private readonly IAgentInfoProvider _info;
    private readonly ReportEnvironment _environment;
    private PendingReportState _state;

    /// <summary>Initializes a new instance of the <see cref="ErrorReportCenterBase"/> class.</summary>
    /// <param name="connection">Sunucu bağlantısı.</param>
    /// <param name="queue">Bekleyen bildirim deposu.</param>
    /// <param name="logs">Son log satırları.</param>
    /// <param name="info">Ajan bilgisi (versiyon).</param>
    /// <param name="environment">Ortam (işletim sistemi; silinecek makine adı ve profil klasörü).</param>
    protected ErrorReportCenterBase(IServerConnection connection, IErrorReportQueue queue, IRecentLogSource logs, IAgentInfoProvider info, ReportEnvironment environment)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _logs = logs ?? throw new ArgumentNullException(nameof(logs));
        _info = info ?? throw new ArgumentNullException(nameof(info));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _state = _queue.Load();
        _connection.StateChanged += (_, state) =>
        {
            if (state == ConnectionState.Connected)
            {
                _ = FlushAsync(CancellationToken.None);
            }
        };
        _connection.SessionRevoked += (_, _) => SetKnownAccount(null);
    }

    /// <inheritdoc />
    public event EventHandler<AgentErrorReport>? AlertRaised;

    /// <inheritdoc />
    public event EventHandler<int>? PendingChanged;

    /// <inheritdoc />
    public event EventHandler<ReportNotice>? Notice;

    /// <inheritdoc />
    public int PendingCount
    {
        get
        {
            lock (_gate)
            {
                return _state.Reports.Count;
            }
        }
    }

    /// <inheritdoc />
    public AgentErrorReport? Raise(string summary, string? component = null, InstallStep? step = null)
    {
        var clean = AgentReportText.Cut(AgentReportText.Sanitize(summary, _environment), AgentReportText.MaxSummaryLength);
        if (string.IsNullOrWhiteSpace(clean))
        {
            return null;
        }

        Guid? account;
        lock (_gate)
        {
            if (!_seen.Add(clean))
            {
                account = null;
                clean = null;
            }
            else
            {
                account = _state.KnownAccount;
            }
        }

        if (clean is null)
        {
            WriteLog(false, "Aynı hata bu ajan oturumunda yeniden oluştu; uyarı yeniden gösterilmedi: " + AgentReportText.Sanitize(summary, _environment));
            return null;
        }

        var now = UtcNow();
        var report = new AgentErrorReport(
            Guid.CreateVersion7(now),
            now,
            account,
            clean,
            _info.Version,
            _environment.OperatingSystem,
            string.IsNullOrWhiteSpace(component) ? null : AgentReportText.Sanitize(component, _environment),
            AgentReportText.StepName(step),
            AgentReportText.SelectLogLines(_logs.Snapshot(), now, _environment));
        AlertRaised?.Invoke(this, report);
        return report;
    }

    /// <inheritdoc />
    public async Task<ReportOutcome> ReportAsync(AgentErrorReport report, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);
        if (_connection.State == ConnectionState.Connected
            && await ReadAccountAsync(cancellationToken).ConfigureAwait(false) is { } account)
        {
            if (report.AccountId is { } owner && owner != account)
            {
                return ReportOutcome.OtherAccount;
            }

            var outcome = await _connection.TrySendErrorReportAsync(report with { AccountId = account }, cancellationToken).ConfigureAwait(false);
            if (outcome is { } sent)
            {
                return sent;
            }
        }

        Enqueue(report);
        return ReportOutcome.Queued;
    }

    /// <inheritdoc />
    public Task SaveToFileAsync(AgentErrorReport report, string path, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return WriteFileAsync(path, AgentReportText.Compose(report), cancellationToken);
    }

    /// <inheritdoc />
    public async Task FlushAsync(CancellationToken cancellationToken)
    {
        await _flush.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Bağlanınca hesap her zaman okunur: sonraki uyarılar oluştukları hesaba bağlansın (Req 8).
            if (_connection.State != ConnectionState.Connected
                || await ReadAccountAsync(cancellationToken).ConfigureAwait(false) is not { } account
                || PendingCount == 0)
            {
                return;
            }

            foreach (var report in Pending())
            {
                if (report.AccountId is { } owner && owner != account)
                {
                    Remove(report);
                    Notice?.Invoke(this, ReportNotice.OtherAccount);
                    continue;
                }

                var outcome = await _connection.TrySendErrorReportAsync(report with { AccountId = account }, cancellationToken).ConfigureAwait(false);
                if (outcome is null)
                {
                    return; // Gönderilemedi: bekletmeye devam, sıradaki bağlantıda yeniden.
                }

                Remove(report);
                switch (outcome)
                {
                    case ReportOutcome.Sent:
                        Notice?.Invoke(this, ReportNotice.Sent);
                        break;
                    case ReportOutcome.OtherAccount:
                        Notice?.Invoke(this, ReportNotice.OtherAccount);
                        break;
                    case ReportOutcome.RateLimited:
                        Notice?.Invoke(this, ReportNotice.RateLimited);
                        break;
                }
            }
        }
        finally
        {
            _flush.Release();
        }
    }

    /// <summary>Teknik loga yazar (Error yazılmaz: uyarı döngüsü olmasın).</summary>
    /// <param name="warning">Warning mi (değilse Information).</param>
    /// <param name="message">Mesaj (maskeli).</param>
    protected abstract void WriteLog(bool warning, string message);

    /// <summary>Dosyaya yazar.</summary>
    /// <param name="path">Dosya.</param>
    /// <param name="text">Metin.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected abstract Task WriteFileAsync(string path, string text, CancellationToken cancellationToken);

    /// <summary>Şimdiki zaman (UTC).</summary>
    /// <returns>Zaman.</returns>
    protected abstract DateTimeOffset UtcNow();

    private async Task<Guid?> ReadAccountAsync(CancellationToken cancellationToken)
    {
        var account = await _connection.TryGetAccountAsync(cancellationToken).ConfigureAwait(false);
        if (account is not null)
        {
            SetKnownAccount(account);
        }

        return account;
    }

    private IReadOnlyList<AgentErrorReport> Pending()
    {
        lock (_gate)
        {
            return _state.Reports;
        }
    }

    private void SetKnownAccount(Guid? account)
    {
        lock (_gate)
        {
            if (_state.KnownAccount == account)
            {
                return;
            }

            _state = _state with { KnownAccount = account };
            _queue.Save(_state);
        }
    }

    private void Enqueue(AgentErrorReport report)
    {
        var dropped = 0;
        int count;
        lock (_gate)
        {
            var reports = _state.Reports.Append(report).ToList();
            while (reports.Count > AgentReportText.MaxPending)
            {
                reports.RemoveAt(0);
                dropped++;
            }

            _state = _state with { Reports = reports };
            _queue.Save(_state);
            count = reports.Count;
        }

        if (dropped > 0)
        {
            WriteLog(true, $"Bekleyen hata bildirimi sınırı ({AgentReportText.MaxPending}) aşıldı; en eski {dropped} bildirim düştü");
        }

        PendingChanged?.Invoke(this, count);
    }

    private void Remove(AgentErrorReport report)
    {
        int count;
        lock (_gate)
        {
            _state = _state with { Reports = _state.Reports.Where(r => r.Id != report.Id).ToList() };
            _queue.Save(_state);
            count = _state.Reports.Count;
        }

        PendingChanged?.Invoke(this, count);
    }
}
