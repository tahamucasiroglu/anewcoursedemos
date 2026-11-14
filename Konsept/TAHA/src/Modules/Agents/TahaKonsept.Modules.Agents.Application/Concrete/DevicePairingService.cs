using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Base;
using TahaKonsept.Modules.Agents.Application.Models;
using TahaKonsept.Modules.Agents.Application.Options;
using TahaKonsept.Modules.Agents.Domain.Entities;
using TahaKonsept.Modules.Agents.Domain.Enums;
using TahaKonsept.Modules.Agents.Domain.Errors;
using TahaKonsept.Shared.Contracts.Agent.Pairing;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Agents.Application.Concrete;

/// <summary>
/// Cihaz eşleme servisi. Başlatmada çakışmayan kullanıcı kodu üretir, cihaz kodunun yalnızca hash'ini saklar; yoklamada durum makinesini
/// (<see cref="DevicePairing.Poll"/>) çalıştırır, onaylıysa cihazı oluşturur/yeniden kullanır (aynı kullanıcı + bilgisayar adı),
/// <see cref="IAgentTokenIssuer"/> ile token üretir ve isteği tek seferlik kapatır.
/// </summary>
public sealed class DevicePairingService : AgentsServiceBase, IDevicePairingService
{
    private const int MaxUserCodeAttempts = 8;

    private readonly IDevicePairingRepository _pairings;
    private readonly IAgentDeviceRepository _devices;
    private readonly IPairingCodeGenerator _codes;
    private readonly IAgentTokenIssuer _tokenIssuer;
    private readonly IVerificationUrlProvider _verificationUrl;
    private readonly ICurrentUser _currentUser;
    private readonly AgentsOptions _options;

    /// <summary>Oluşturur.</summary>
    /// <param name="pairings">Eşleme deposu.</param>
    /// <param name="devices">Cihaz deposu.</param>
    /// <param name="codes">Kod üretici.</param>
    /// <param name="tokenIssuer">Token üretici.</param>
    /// <param name="verificationUrl">Doğrulama adresi sağlayıcı.</param>
    /// <param name="currentUser">Oturum kullanıcısı.</param>
    /// <param name="options">Ayarlar.</param>
    /// <param name="clock">Saat.</param>
    public DevicePairingService(
        IDevicePairingRepository pairings,
        IAgentDeviceRepository devices,
        IPairingCodeGenerator codes,
        IAgentTokenIssuer tokenIssuer,
        IVerificationUrlProvider verificationUrl,
        ICurrentUser currentUser,
        AgentsOptions options,
        IClock clock)
        : base(clock)
    {
        _pairings = pairings ?? throw new ArgumentNullException(nameof(pairings));
        _devices = devices ?? throw new ArgumentNullException(nameof(devices));
        _codes = codes ?? throw new ArgumentNullException(nameof(codes));
        _tokenIssuer = tokenIssuer ?? throw new ArgumentNullException(nameof(tokenIssuer));
        _verificationUrl = verificationUrl ?? throw new ArgumentNullException(nameof(verificationUrl));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>Eşleme isteği açar.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Başlatma yanıtı.</returns>
    public async Task<Result<DeviceStartResponse>> StartAsync(DeviceStartRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var now = Now;
        await _pairings.DeleteExpiredAsync(now.AddHours(-1), cancellationToken);

        var userCode = await CreateUniqueUserCodeAsync(now, cancellationToken);
        var deviceCode = _codes.CreateDeviceCode();
        var lifetime = TimeSpan.FromSeconds(Math.Max(60, _options.PairingLifetimeSeconds));
        var pairing = DevicePairing.Start(
            _codes.HashDeviceCode(deviceCode),
            userCode,
            request.MachineName,
            request.AgentVersion,
            now,
            lifetime,
            _options.PollIntervalSeconds);

        await _pairings.AddAsync(pairing, cancellationToken);
        await _pairings.UnitOfWork.SaveChangesAsync(cancellationToken);

        var formatted = _codes.FormatUserCode(userCode);
        var verificationUrl = _verificationUrl.GetVerificationUrl();
        return new DeviceStartResponse
        {
            DeviceCode = deviceCode,
            UserCode = formatted,
            VerificationUrl = verificationUrl,
            VerificationUrlComplete = AppendCode(verificationUrl, formatted),
            IntervalSeconds = pairing.IntervalSeconds,
            ExpiresInSeconds = (int)lifetime.TotalSeconds,
        };
    }

    /// <summary>Ajan token yoklaması.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Token ya da RFC 8628 hata kodu.</returns>
    public async Task<Result<TokenResponse>> PollTokenAsync(DeviceTokenRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.DeviceCode))
        {
            return AgentErrors.InvalidDeviceCode;
        }

        var pairing = await _pairings.FindByDeviceCodeHashAsync(_codes.HashDeviceCode(request.DeviceCode), cancellationToken);
        if (pairing is null)
        {
            return AgentErrors.InvalidDeviceCode;
        }

        var now = Now;
        var outcome = pairing.Poll(now);
        if (outcome != PairingPollOutcome.Approved)
        {
            await _pairings.UnitOfWork.SaveChangesAsync(cancellationToken);
            return outcome switch
            {
                PairingPollOutcome.Pending => AgentErrors.AuthorizationPending,
                PairingPollOutcome.SlowDown => AgentErrors.SlowDown,
                PairingPollOutcome.Denied => AgentErrors.AccessDenied,
                _ => AgentErrors.ExpiredToken,
            };
        }

        var device = await _devices.FindActiveByMachineAsync(pairing.UserId, pairing.MachineName, cancellationToken);
        if (device is null)
        {
            device = AgentDevice.Pair(pairing.UserId, pairing.MachineName, pairing.AgentVersion, now);
            await _devices.AddAsync(device, cancellationToken);
        }
        else
        {
            device.Repair(pairing.AgentVersion, now);
        }

        var tokens = await _tokenIssuer.IssueAsync(new AgentTokenRequest(pairing.UserId, device.Id, device.MachineName), cancellationToken);
        if (tokens.IsFailure)
        {
            return tokens.Error;
        }

        var consumed = pairing.Consume(device.Id, now);
        if (consumed.IsFailure)
        {
            return consumed.Error;
        }

        var previousFamily = device.AssignTokenFamily(tokens.Value.TokenFamilyId);
        await _pairings.UnitOfWork.SaveChangesAsync(cancellationToken);
        if (previousFamily is { } oldFamily && oldFamily != tokens.Value.TokenFamilyId)
        {
            // Aynı bilgisayar yeniden eşlendi: eski ajan oturumu geçersiz.
            await _tokenIssuer.RevokeAsync(pairing.UserId, oldFamily, cancellationToken);
        }

        return tokens.Value.Tokens;
    }

    /// <summary>Kodu onaylar.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Onaylanan bilgisayar.</returns>
    public async Task<Result<DeviceApproveResponse>> ApproveAsync(DeviceApproveRequest request, CancellationToken cancellationToken = default)
    {
        var decided = await DecideAsync(request, approve: true, cancellationToken);
        return decided.Map(p => new DeviceApproveResponse { MachineName = p.MachineName, AgentVersion = p.AgentVersion });
    }

    /// <summary>Kodu reddeder.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public async Task<Result> DenyAsync(DeviceApproveRequest request, CancellationToken cancellationToken = default)
    {
        var decided = await DecideAsync(request, approve: false, cancellationToken);
        return decided.IsSuccess ? Result.Success() : Result.Failure(decided.Error);
    }

    private static string AppendCode(string url, string formattedCode)
    {
        var hashIndex = url.IndexOf('#', StringComparison.Ordinal);
        var basePart = hashIndex >= 0 ? url[..hashIndex] : url;
        var fragment = hashIndex >= 0 ? url[hashIndex..] : string.Empty;
        var separator = basePart.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return basePart + separator + "kod=" + Uri.EscapeDataString(formattedCode) + fragment;
    }

    private async Task<Result<DevicePairing>> DecideAsync(DeviceApproveRequest request, bool approve, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = RequireUser(_currentUser);
        if (user.IsFailure)
        {
            return user.Error;
        }

        var code = _codes.NormalizeUserCode(request.UserCode);
        if (code.Length != PairingCodeGeneratorBase.UserCodeLength)
        {
            return AgentErrors.UserCodeNotFound;
        }

        var pairing = await _pairings.FindOpenByUserCodeAsync(code, cancellationToken);
        if (pairing is null)
        {
            return AgentErrors.UserCodeNotFound;
        }

        var now = Now;
        var result = approve ? pairing.Approve(user.Value, now) : pairing.Deny(user.Value, now);
        if (result.IsFailure)
        {
            return result.Error;
        }

        await _pairings.UnitOfWork.SaveChangesAsync(cancellationToken);
        return pairing;
    }

    private async Task<string> CreateUniqueUserCodeAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxUserCodeAttempts; attempt++)
        {
            var candidate = _codes.CreateUserCode();
            if (!await _pairings.IsUserCodeInUseAsync(candidate, now, cancellationToken))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Benzersiz eşleme kodu üretilemedi.");
    }
}
