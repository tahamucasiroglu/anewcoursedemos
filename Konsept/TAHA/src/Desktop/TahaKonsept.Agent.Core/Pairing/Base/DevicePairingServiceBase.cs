using TahaKonsept.Agent.Core.Auth.Abstract;
using TahaKonsept.Agent.Core.Http.Abstract;
using TahaKonsept.Agent.Core.Pairing.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Agent.Pairing;

namespace TahaKonsept.Agent.Core.Pairing.Base;

/// <summary>
/// Cihaz eşleme durum makinesi: <c>authorization_pending</c> → yoklamaya devam, <c>slow_down</c> → aralığı artır,
/// <c>expired_token</c> → <see cref="PairingState.Expired"/>, <c>access_denied</c> → <see cref="PairingState.Denied"/>,
/// başarı → token'ları kaydet ve <see cref="PairingState.Paired"/>. Geçici ağ hatalarında yoklama sürer.
/// </summary>
public abstract class DevicePairingServiceBase : IDevicePairingService
{
    private readonly IAnonymousAgentApiClient _api;
    private readonly IAgentAuthService _auth;
    private PairingStatus _status;

    /// <summary>Oluşturur.</summary>
    /// <param name="api">Anonim API istemcisi.</param>
    /// <param name="auth">Oturum servisi.</param>
    /// <param name="timeProvider">Zaman sağlayıcı (bekleme ve süre hesabı).</param>
    /// <param name="slowDownStep"><c>slow_down</c> geldiğinde eklenecek süre.</param>
    protected DevicePairingServiceBase(IAnonymousAgentApiClient api, IAgentAuthService auth, TimeProvider timeProvider, TimeSpan slowDownStep)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _auth = auth ?? throw new ArgumentNullException(nameof(auth));
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        SlowDownStep = slowDownStep;
        _status = auth.IsPaired
            ? new PairingStatus(PairingState.Paired, "Bu bilgisayar hesabınızla eşlendi.")
            : new PairingStatus(PairingState.NotPaired, "Bu bilgisayar henüz eşlenmedi.");
    }

    /// <inheritdoc />
    public event EventHandler<PairingStatus>? StatusChanged;

    /// <summary>Zaman sağlayıcı.</summary>
    protected TimeProvider TimeProvider { get; }

    /// <summary><c>slow_down</c> artışı.</summary>
    protected TimeSpan SlowDownStep { get; }

    /// <inheritdoc />
    public PairingStatus Status => Volatile.Read(ref _status);

    /// <inheritdoc />
    public async Task<Result<DeviceStartResponse>> StartAsync(CancellationToken ct)
    {
        SetStatus(new PairingStatus(PairingState.Starting, "Eşleme kodu alınıyor…"));
        var result = await _api.StartDeviceAsync(CreateStartRequest(), ct);
        if (result.IsFailure)
        {
            SetStatus(new PairingStatus(PairingState.Failed, "Eşleme başlatılamadı: " + result.Error.Message));
            return result.Error;
        }

        var start = result.Value;
        SetStatus(new PairingStatus(
            PairingState.WaitingForUser,
            "Web'de Ayarlar → Masaüstü Programı sayfasında bu kodu onaylayın.",
            start.UserCode,
            start.VerificationUrl,
            TimeProvider.GetUtcNow().AddSeconds(Math.Max(1, start.ExpiresInSeconds))));
        return start;
    }

    /// <inheritdoc />
    public async Task<Result> WaitForApprovalAsync(DeviceStartResponse start, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(start);
        var interval = TimeSpan.FromSeconds(Math.Max(0, start.IntervalSeconds));
        var expiresAt = TimeProvider.GetUtcNow().AddSeconds(Math.Max(1, start.ExpiresInSeconds));
        var request = new DeviceTokenRequest { DeviceCode = start.DeviceCode };

        try
        {
            while (true)
            {
                if (TimeProvider.GetUtcNow() >= expiresAt)
                {
                    return Expire();
                }

                await Task.Delay(interval, TimeProvider, ct);
                var poll = await _api.RequestDeviceTokenAsync(request, ct);
                if (poll.IsSuccess)
                {
                    _auth.SetTokens(poll.Value);
                    SetStatus(new PairingStatus(PairingState.Paired, "Eşleme tamamlandı."));
                    OnPaired();
                    return Result.Success();
                }

                switch (poll.Error.Code)
                {
                    case DeviceTokenErrorCodes.AuthorizationPending:
                        continue;
                    case DeviceTokenErrorCodes.SlowDown:
                        interval += SlowDownStep;
                        continue;
                    case DeviceTokenErrorCodes.ExpiredToken:
                        return Expire();
                    case DeviceTokenErrorCodes.AccessDenied:
                        SetStatus(new PairingStatus(PairingState.Denied, "Eşleme reddedildi."));
                        return poll.Error;
                    default:
                        if (IsTransient(poll.Error))
                        {
                            OnTransientPollError(poll.Error);
                            continue;
                        }

                        SetStatus(new PairingStatus(PairingState.Failed, "Eşleme başarısız: " + poll.Error.Message));
                        return poll.Error;
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            SetStatus(new PairingStatus(PairingState.Cancelled, "Eşleme iptal edildi."));
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Result> PairAsync(CancellationToken ct)
    {
        var start = await StartAsync(ct);
        return start.IsFailure ? start.Error : await WaitForApprovalAsync(start.Value, ct);
    }

    /// <inheritdoc />
    public void Unpair()
    {
        _auth.SignOut();
        SetStatus(new PairingStatus(PairingState.NotPaired, "Eşleme kaldırıldı."));
    }

    /// <summary>Başlatma isteğini üretir (makine adı, sürüm).</summary>
    /// <returns>İstek.</returns>
    protected abstract DeviceStartRequest CreateStartRequest();

    /// <summary>Eşleme tamamlanınca çağrılır.</summary>
    protected virtual void OnPaired()
    {
    }

    /// <summary>Geçici yoklama hatasında çağrılır (yoklama sürer).</summary>
    /// <param name="error">Hata.</param>
    protected virtual void OnTransientPollError(Error error)
    {
    }

    /// <summary>Durumu değiştirir ve olayı tetikler.</summary>
    /// <param name="status">Yeni durum.</param>
    protected void SetStatus(PairingStatus status)
    {
        Volatile.Write(ref _status, status);
        StatusChanged?.Invoke(this, status);
    }

    private Error Expire()
    {
        SetStatus(new PairingStatus(PairingState.Expired, "Kodun süresi doldu. Yeniden deneyin."));
        return Error.Failure(DeviceTokenErrorCodes.ExpiredToken, "Eşleme kodunun süresi doldu.");
    }

    private static bool IsTransient(Error error) =>
        error.Type == ErrorType.Failure && (error.Code.StartsWith("http.", StringComparison.Ordinal));
}
