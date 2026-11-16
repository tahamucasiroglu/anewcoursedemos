using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Application.Dtos;
using Konsept.Modules.Identity.Application.Resources;
using Konsept.Modules.Identity.Domain.Entities;
using Konsept.Modules.Identity.Domain.Enums;
using Konsept.Modules.Identity.Domain.Rules;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Modules.Monitoring.Contracts.Enums;
using Konsept.Shared.Kernel.Enums;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Identity.Application.Base;

/// <summary>
/// Eşleştirme akışı (spec 0012 Req 1–8, 14): kod üretimi, ajanın durum sorgusu (belirteç bir kez), web'den sorgu/onay/ret.
/// Onay kod deposunun kilidi altında başlar (çift tıklama tek onay, AC-10); önceki cihazlar iptal edilir (Req 6), yeni cihaz
/// kaydedilince belirteç teslime hazır olur; kayıt başarısızsa kod yeniden karar bekler. Olay yan etkidir, kod/sır/belirteç
/// içermez. Rastgele kod kaynağı ve olay hatası loglaması alt sınıftadır.
/// </summary>
/// <param name="dependencies">Bağımlılıklar.</param>
public abstract class AgentPairingServiceBase(AgentPairingDependencies dependencies) : IAgentPairingService
{
    /// <summary>Makine adının en uzun hali.</summary>
    public const int MaxMachineNameLength = 64;

    /// <summary>Ajan sürümünün en uzun hali.</summary>
    public const int MaxAgentVersionLength = 32;

    /// <summary>Gets bağımlılıklar.</summary>
    protected AgentPairingDependencies Dependencies { get; } = dependencies ?? throw new ArgumentNullException(nameof(dependencies));

    private DateTimeOffset Now => Dependencies.TimeProvider.GetUtcNow();

    /// <inheritdoc />
    public Task<Result<PairingCodeDto>> CreateCodeAsync(CreatePairingCodeRequest request, CancellationToken cancellationToken)
    {
        var machine = request?.MachineName?.Trim() ?? string.Empty;
        var version = request?.AgentVersion?.Trim() ?? string.Empty;
        if (machine.Length is 0 or > MaxMachineNameLength)
        {
            return Task.FromResult(Result<PairingCodeDto>.Failure(new Error("agent_pairing.machine_name", IdentityText.MachineNameRequired, ErrorType.Validation)));
        }

        if (version.Length is 0 or > MaxAgentVersionLength)
        {
            return Task.FromResult(Result<PairingCodeDto>.Failure(new Error("agent_pairing.agent_version", IdentityText.AgentVersionRequired, ErrorType.Validation)));
        }

        var now = Now;
        string code;
        do
        {
            code = PairingCode.Generate(NextIndex);
        }
        while (Dependencies.Store.Contains(code));

        var secret = Dependencies.Tokens.Create();
        var pairing = PairingRequest.Open(code, Dependencies.Tokens.Hash(secret), machine, version, now);
        Dependencies.Store.Add(pairing, now);
        return Task.FromResult(Result<PairingCodeDto>.Success(new PairingCodeDto(code, secret, pairing.ExpiresAt)));
    }

    /// <inheritdoc />
    public Task<Result<PairingStatusDto>> GetStatusAsync(PairingStatusRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Secret))
        {
            return Task.FromResult(Result<PairingStatusDto>.Success(new PairingStatusDto(PairingStatusValues.Expired)));
        }

        var now = Now;
        var status = Dependencies.Store.WithSecretHash(Dependencies.Tokens.Hash(request.Secret), r => r switch
        {
            null => new PairingStatusDto(PairingStatusValues.Expired),
            { State: PairingState.Rejected } => new PairingStatusDto(PairingStatusValues.Rejected),
            { State: PairingState.Approving } => new PairingStatusDto(PairingStatusValues.Pending),
            { State: PairingState.Approved } => new PairingStatusDto(PairingStatusValues.Approved, r.TakeToken()),
            { State: PairingState.Pending } when !r.IsExpired(now) => new PairingStatusDto(PairingStatusValues.Pending),
            _ => new PairingStatusDto(PairingStatusValues.Expired),
        });
        return Task.FromResult(Result<PairingStatusDto>.Success(status));
    }

    /// <inheritdoc />
    public Task<Result<PairingDeviceDto>> LookupAsync(string? code, CancellationToken cancellationToken)
    {
        var now = Now;
        return Task.FromResult(Decide(code, r => r.CanDecide(now)).Result);
    }

    /// <inheritdoc />
    public async Task<Result<PairingDeviceDto>> ApproveAsync(string? code, CancellationToken cancellationToken)
    {
        if (Dependencies.CurrentUser.UserId is not { } userId)
        {
            return Result<PairingDeviceDto>.Failure(IdentityErrors.SessionInvalid);
        }

        var now = Now;
        var (result, canonical) = Decide(code, r => r.BeginApproval(userId, now));
        if (!result.IsSuccess)
        {
            return result;
        }

        var device = result.Value;
        var token = Dependencies.Tokens.Create();
        try
        {
            // Kullanıcı başına tek ajan (Req 6): önceki ajanın oturumu iptal edilir ve canlı bağlantısı kapatılır.
            await Dependencies.Revoker.RevokeAllAsync(userId, cancellationToken).ConfigureAwait(false);
            await Dependencies.Devices.AddAsync(
                AgentDevice.Pair(userId, device.MachineName, device.AgentVersion, Dependencies.Tokens.Hash(token), now),
                cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            Dependencies.Store.WithCode(canonical!, r =>
            {
                r?.AbortApproval();
                return 0;
            });
            throw;
        }

        Dependencies.Store.WithCode(canonical!, r =>
        {
            r?.CompleteApproval(token);
            return 0;
        });
        await WriteEventAsync(userId, device, cancellationToken).ConfigureAwait(false);
        return result;
    }

    /// <inheritdoc />
    public Task<Result<PairingDeviceDto>> RejectAsync(string? code, CancellationToken cancellationToken)
    {
        var now = Now;
        return Task.FromResult(Decide(code, r => r.Reject(now)).Result);
    }

    /// <summary>Kriptografik rastgele indeks üretir.</summary>
    /// <param name="maxExclusive">Üst sınır (hariç).</param>
    /// <returns>İndeks.</returns>
    protected abstract int NextIndex(int maxExclusive);

    /// <summary>Olay yazılamadığında çağrılır (eşleştirme bozulmaz).</summary>
    /// <param name="exception">Hata.</param>
    protected abstract void OnEventWriteFailed(Exception exception);

    private static Result<PairingDeviceDto> ToResult(PairingDecision decision, PairingDeviceDto device) => decision switch
    {
        PairingDecision.Accepted => Result<PairingDeviceDto>.Success(device),
        PairingDecision.Expired => Result<PairingDeviceDto>.Failure(IdentityErrors.PairingCodeExpired),
        _ => Result<PairingDeviceDto>.Failure(IdentityErrors.PairingCodeUsed),
    };

    private (Result<PairingDeviceDto> Result, string? Canonical) Decide(string? code, Func<PairingRequest, PairingDecision> decide)
    {
        var canonical = PairingCode.Normalize(code);
        if (canonical is null)
        {
            return (Result<PairingDeviceDto>.Failure(IdentityErrors.PairingCodeNotFound), null);
        }

        var result = Dependencies.Store.WithCode(canonical, r => r is null
            ? Result<PairingDeviceDto>.Failure(IdentityErrors.PairingCodeNotFound)
            : ToResult(decide(r), new PairingDeviceDto(r.Code, r.MachineName, r.AgentVersion, r.ExpiresAt)));
        return (result, canonical);
    }

    private async Task WriteEventAsync(Guid userId, PairingDeviceDto device, CancellationToken cancellationToken)
    {
        try
        {
            await Dependencies.Events.WriteAsync(
                new SystemEventEntry(IdentityEvents.AgentPaired, SystemEventLevel.Info, IdentityEvents.AgentPairedMessage(device.MachineName, device.AgentVersion), userId),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            OnEventWriteFailed(ex);
        }
    }
}
