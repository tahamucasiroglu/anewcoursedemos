using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Base;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Domain.Entities;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Concrete;

/// <summary>
/// <see cref="IApiKeyService"/> uygulaması: anahtarları <see cref="ISecretProtector"/> ile şifreleyip saklar, yalnızca son 4 haneyi
/// gösterir, test sonucunu kaydeder. Platform kimlikleri küçük harfe normalize edilir.
/// </summary>
public sealed class ApiKeyService : ProvidersServiceBase, IApiKeyService
{
    private readonly ICurrentUser _currentUser;
    private readonly IProviderApiKeyRepository _keys;
    private readonly ISecretProtector _protector;
    private readonly IApiKeyVault _vault;
    private readonly IApiKeyTester _tester;

    /// <summary>Kurucu.</summary>
    /// <param name="currentUser">Oturumdaki kullanıcı.</param>
    /// <param name="keys">Anahtar deposu.</param>
    /// <param name="protector">Şifreleyici.</param>
    /// <param name="vault">Anahtar kasası.</param>
    /// <param name="tester">Anahtar test edici.</param>
    /// <param name="clock">Saat.</param>
    public ApiKeyService(
        ICurrentUser currentUser,
        IProviderApiKeyRepository keys,
        ISecretProtector protector,
        IApiKeyVault vault,
        IApiKeyTester tester,
        IClock clock)
        : base(clock)
    {
        _currentUser = currentUser;
        _keys = keys;
        _protector = protector;
        _vault = vault;
        _tester = tester;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ApiKeyInfo>>> ListAsync(CancellationToken ct)
    {
        var userId = _currentUser.GetRequiredUserId();
        var saved = await _keys.ListAsync(k => k.UserId == userId, ct);
        IReadOnlyList<ApiKeyInfo> list = ProviderPlatformDirectory.All
            .Select(d => ToInfo(d, saved.FirstOrDefault(k => k.Platform == d.Platform)))
            .ToList();
        return Result.Success(list);
    }

    /// <inheritdoc />
    public async Task<Result<ApiKeyInfo>> SaveAsync(SaveApiKeyRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var descriptor = ProviderPlatformDirectory.Find(request.Platform);
        if (descriptor is null)
        {
            return Error.Validation(ProviderErrorCodes.UnknownPlatform, $"Bilinmeyen platform: {request.Platform}");
        }

        var apiKey = request.ApiKey.Trim();
        var region = descriptor.RequiresRegion ? request.Region?.Trim().ToLowerInvariant() : null;
        if (descriptor.RequiresRegion && string.IsNullOrWhiteSpace(region))
        {
            return Error.Validation(ProviderErrorCodes.InvalidRequest, $"{descriptor.DisplayName} için bölge gerekli (ör. westeurope).");
        }

        var userId = _currentUser.GetRequiredUserId();
        var protectedKey = _protector.Protect(apiKey);
        var last4 = apiKey.Length <= 4 ? apiKey : apiKey[^4..];
        var entity = await _keys.FindAsync(userId, descriptor.Platform, ct);
        if (entity is null)
        {
            entity = ProviderApiKey.Create(userId, descriptor.Platform, protectedKey, last4, region);
            await _keys.AddAsync(entity, ct);
        }
        else
        {
            entity.Replace(protectedKey, last4, region);
        }

        await _keys.UnitOfWork.SaveChangesAsync(ct);
        return ToInfo(descriptor, entity);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(string platform, CancellationToken ct)
    {
        var descriptor = ProviderPlatformDirectory.Find(platform);
        if (descriptor is null)
        {
            return Error.Validation(ProviderErrorCodes.UnknownPlatform, $"Bilinmeyen platform: {platform}");
        }

        var entity = await _keys.FindAsync(_currentUser.GetRequiredUserId(), descriptor.Platform, ct);
        if (entity is null)
        {
            return Error.NotFound(ProviderErrorCodes.NotFound, $"{descriptor.DisplayName} için kayıtlı anahtar yok.");
        }

        _keys.Remove(entity);
        await _keys.UnitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<ApiKeyTestResult>> TestAsync(string platform, CancellationToken ct)
    {
        var userId = _currentUser.GetRequiredUserId();
        var credential = await _vault.GetCredentialAsync(userId, platform, ct);
        if (credential.IsFailure)
        {
            return credential.Error;
        }

        var result = await _tester.TestAsync(credential.Value, ct);
        var entity = await _keys.FindAsync(userId, credential.Value.Platform, ct);
        if (entity is not null)
        {
            entity.RecordTest(result.Success, Clock.UtcNow);
            await _keys.UnitOfWork.SaveChangesAsync(ct);
        }

        return result;
    }

    /// <summary>Satır DTO'su oluşturur.</summary>
    /// <param name="descriptor">Platform tanımı.</param>
    /// <param name="entity">Kayıt (yoksa null).</param>
    private static ApiKeyInfo ToInfo(PlatformDescriptor descriptor, ProviderApiKey? entity) => new()
    {
        Platform = descriptor.Platform,
        DisplayName = descriptor.DisplayName,
        Description = descriptor.Description,
        RequiresRegion = descriptor.RequiresRegion,
        Configured = entity is not null,
        Last4 = entity?.Last4,
        Region = entity?.Region,
        UpdatedAtUtc = entity?.UpdatedAtUtc,
        LastTestSucceeded = entity?.LastTestSucceeded,
        LastTestedAtUtc = entity?.LastTestedAtUtc,
    };
}
