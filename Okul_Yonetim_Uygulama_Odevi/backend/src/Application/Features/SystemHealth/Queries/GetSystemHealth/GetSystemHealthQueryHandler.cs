using Microsoft.Extensions.Logging;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Application.Features.SystemHealth.Queries.GetSystemHealth;

/// <summary>
/// Sistem durumunu belirler: veritabanına en fazla 3 saniye içinde ulaşılırsa "çalışıyor", aksi halde
/// "hizmet dışı". Mesaj nArchitecture YAML yerelleştirmesinden (SystemHealth bölümü = özellik klasörü adı, tr) gelir;
/// "hizmet dışı" sonucu Warning olarak loglanır.
/// </summary>
public sealed class GetSystemHealthQueryHandler : IRequestHandler<GetSystemHealthQuery, SystemHealthResponse>
{
    public const int DatabaseTimeoutSeconds = 3;
    public static readonly TimeSpan DatabaseTimeout = TimeSpan.FromSeconds(DatabaseTimeoutSeconds);

    private const string LocaleSection = "SystemHealth";
    private static readonly string[] Locales = ["tr"];

    private readonly IDatabaseHealthProbe probe;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<GetSystemHealthQueryHandler> logger;

    public GetSystemHealthQueryHandler(
        IDatabaseHealthProbe probe,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<GetSystemHealthQueryHandler> logger)
    {
        this.probe = probe;
        this.localization = localization;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<SystemHealthResponse> Handle(GetSystemHealthQuery request, CancellationToken cancellationToken)
    {
        bool reachable = await this.IsDatabaseReachableAsync(cancellationToken).ConfigureAwait(false);
        SystemHealthStatus status = reachable ? SystemHealthStatus.Healthy : SystemHealthStatus.Unavailable;
        string message = await this.localization
            .GetLocalizedAsync(status.ToString(), Locales, LocaleSection)
            .ConfigureAwait(false);

        SystemHealthResponse response = SystemHealthResponse.From(status, message);
        string statusText = response.Status;
        if (reachable)
        {
            this.logger.LogInformation("Sistem durumu: {Status}", statusText);
        }
        else
        {
            this.logger.LogWarning("Sistem durumu: {Status} — veritabanına {TimeoutSeconds} sn içinde ulaşılamadı", statusText, DatabaseTimeoutSeconds);
        }

        return response;
    }

    private async Task<bool> IsDatabaseReachableAsync(CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeout = new(DatabaseTimeout, this.timeProvider);
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        Task<bool> probeTask = this.probe.CanConnectAsync(linked.Token);
        Task timeoutTask = Task.Delay(Timeout.InfiniteTimeSpan, linked.Token);
        Task finished = await Task.WhenAny(probeTask, timeoutTask).ConfigureAwait(false);

        if (finished != probeTask)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return false;
        }

        try
        {
            return await probeTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }
}
