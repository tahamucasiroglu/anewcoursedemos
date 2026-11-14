using TahaKonsept.Agent.Core.Pairing;
using TahaKonsept.Agent.Core.Pairing.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Agent.Pairing;

namespace TahaKonsept.Agent.Tests.Fakes;

/// <summary>
/// Yapılandırılabilir sahte eşleme servisi: <see cref="StartResult"/> ve <see cref="WaitResult"/> ne dönecek, durum geçişleri
/// gerçek servisteki gibi <see cref="StatusChanged"/> ile yayınlanır. <see cref="WaitGate"/> verilirse onay beklemesi o tamamlanana dek sürer.
/// </summary>
public sealed class FakePairingService : IDevicePairingService
{
    /// <summary>Varsayılan başlatma yanıtı.</summary>
    public static readonly DeviceStartResponse DefaultStart = new()
    {
        DeviceCode = "dev-123",
        UserCode = "ABCD-EFGH",
        VerificationUrl = "http://192.168.1.102:7110/Ayarlar/Cihaz",
        VerificationUrlComplete = "http://192.168.1.102:7110/Ayarlar/Cihaz?kod=ABCD-EFGH",
        IntervalSeconds = 5,
        ExpiresInSeconds = 600,
    };

    /// <inheritdoc />
    public event EventHandler<PairingStatus>? StatusChanged;

    /// <inheritdoc />
    public PairingStatus Status { get; private set; } = new(PairingState.NotPaired, "Bu bilgisayar henüz eşlenmedi.");

    /// <summary><see cref="StartAsync"/> sonucu.</summary>
    public Result<DeviceStartResponse> StartResult { get; set; } = Result.Success(DefaultStart);

    /// <summary><see cref="WaitForApprovalAsync"/> sonucu.</summary>
    public Result WaitResult { get; set; } = Result.Success();

    /// <summary>Verilirse onay beklemesi bu görev tamamlanana (ya da iptal) kadar sürer.</summary>
    public TaskCompletionSource? WaitGate { get; set; }

    /// <summary>Başlatma çağrı sayısı.</summary>
    public int StartCalls { get; private set; }

    /// <summary>Unpair çağrıldı mı.</summary>
    public bool Unpaired { get; private set; }

    /// <inheritdoc />
    public Task<Result<DeviceStartResponse>> StartAsync(CancellationToken ct)
    {
        StartCalls++;
        Set(new PairingStatus(PairingState.Starting, "Eşleme kodu alınıyor…"));
        if (StartResult.IsFailure)
        {
            Set(new PairingStatus(PairingState.Failed, "Eşleme başlatılamadı: " + StartResult.Error.Message));
            return Task.FromResult(StartResult);
        }

        var r = StartResult.Value;
        Set(new PairingStatus(PairingState.WaitingForUser, "Kodu web'de onaylayın.", r.UserCode, r.VerificationUrl));
        return Task.FromResult(StartResult);
    }

    /// <inheritdoc />
    public async Task<Result> WaitForApprovalAsync(DeviceStartResponse start, CancellationToken ct)
    {
        if (WaitGate is not null)
        {
            try
            {
                await WaitGate.Task.WaitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                Set(new PairingStatus(PairingState.Cancelled, "Eşleme iptal edildi."));
                return Result.Failure(Error.Failure("cancelled", "İptal"));
            }
        }

        if (WaitResult.IsSuccess)
        {
            Set(new PairingStatus(PairingState.Paired, "Eşleme tamamlandı."));
        }
        else if (WaitResult.Error.Code == DeviceTokenErrorCodes.AccessDenied)
        {
            Set(new PairingStatus(PairingState.Denied, "Eşleme reddedildi."));
        }
        else
        {
            Set(new PairingStatus(PairingState.Failed, "Eşleme başarısız: " + WaitResult.Error.Message));
        }

        return WaitResult;
    }

    /// <inheritdoc />
    public async Task<Result> PairAsync(CancellationToken ct)
    {
        var start = await StartAsync(ct);
        return start.IsFailure ? Result.Failure(start.Error) : await WaitForApprovalAsync(start.Value, ct);
    }

    /// <inheritdoc />
    public void Unpair()
    {
        Unpaired = true;
        Set(new PairingStatus(PairingState.NotPaired, "Eşleme kaldırıldı."));
    }

    private void Set(PairingStatus status)
    {
        Status = status;
        StatusChanged?.Invoke(this, status);
    }
}
