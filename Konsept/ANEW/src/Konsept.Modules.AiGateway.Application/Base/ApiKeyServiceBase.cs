using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Application.Dtos;
using Konsept.Modules.AiGateway.Application.Validators;
using Konsept.Modules.AiGateway.Domain.Entities;
using Konsept.Modules.AiGateway.Domain.Enums;
using Konsept.Modules.AiGateway.Domain.Rules;
using Konsept.Shared.Kernel.Abstract;
using Konsept.Shared.Kernel.Results;
using Konsept.Shared.Kernel.Security;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.AiGateway.Application.Base;

/// <summary>
/// API key yönetimi (spec 0004 Req 7–12, H-2). Her işlem isteği yapan kullanıcının kimliğiyle yapılır (BR-1); URL'de yalnızca
/// sağlayıcı adı vardır, başka kullanıcının satırına hiçbir yol yoktur. Kaydetme: biçim → sağlayıcıda tek maliyetsiz doğrulama →
/// 401 ise kaydetmeden red (mevcut key değişmez), değilse şifreleyip yerine koyma. Kaydedilen key bilinen sır olur (B-45):
/// bir log metnine nasıl girerse girsin 0008 maskesi onu gizler. Loglarda ve yanıtlarda yalnızca maskeli hal bulunur.
/// </summary>
/// <param name="catalog">Katalog.</param>
/// <param name="keys">Key deposu.</param>
/// <param name="protector">Şifreleme.</param>
/// <param name="validator">Sağlayıcı doğrulaması.</param>
/// <param name="currentUser">İsteği yapan kullanıcı.</param>
/// <param name="time">Saat.</param>
/// <param name="logger">Log.</param>
public abstract class ApiKeyServiceBase(
    ICatalogSource catalog,
    IApiKeyRepository keys,
    IApiKeyProtector protector,
    IProviderKeyValidator validator,
    ICurrentUser currentUser,
    TimeProvider time,
    ILogger logger) : IApiKeyService
{
    private static readonly Action<ILogger, string, string, string, Exception?> KeySaved = LoggerMessage.Define<string, string, string>(
        LogLevel.Information,
        new EventId(4001, "ApiKeySaved"),
        "API key kaydedildi: {Provider} {KeyHint} ({Validation})");

    private static readonly Action<ILogger, string, string, Exception?> KeyRejected = LoggerMessage.Define<string, string>(
        LogLevel.Warning,
        new EventId(4002, "ApiKeyRejected"),
        "Sağlayıcı API key'i reddetti, kaydedilmedi: {Provider} {KeyHint}");

    private static readonly Action<ILogger, string, Exception?> KeyDeleted = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(4003, "ApiKeyDeleted"),
        "API key silindi: {Provider}");

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ApiKeyStatusDto>>> ListAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<ApiKeyStatusDto>>.Failure(AiGatewayErrors.SignInRequired);
        }

        var rows = await keys.ListAsync(userId, cancellationToken).ConfigureAwait(false);
        var list = catalog.Catalog.Providers
            .Select(p => rows.FirstOrDefault(r => r.ProviderId == p.Id) is { } row ? StatusOf(p, row) : Empty(p))
            .ToList();
        return Result<IReadOnlyList<ApiKeyStatusDto>>.Success(list);
    }

    /// <inheritdoc />
    public async Task<Result<ApiKeyStatusDto>> SaveAsync(string providerId, SaveApiKeyRequest? request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<ApiKeyStatusDto>.Failure(AiGatewayErrors.SignInRequired);
        }

        if (FindProvider(providerId) is not { } provider)
        {
            return Result<ApiKeyStatusDto>.Failure(AiGatewayErrors.UnknownProvider);
        }

        // Sınırda validator zaten çalışır; servis de aynı kuralı uygular (doğrudan çağrıda da kural delinmez).
        if (SaveApiKeyRequestValidator.MessageFor(ApiKeyFormat.Check(request?.ApiKey)) is { } invalid)
        {
            return Result<ApiKeyStatusDto>.Failure(AiGatewayErrors.KeyInvalid(invalid));
        }

        var key = ApiKeyFormat.Normalize(request!.ApiKey);
        var hint = ApiKeyMask.Mask(key);
        var outcome = await validator.CheckAsync(provider, key, cancellationToken).ConfigureAwait(false);
        if (outcome == KeyCheckOutcome.Rejected)
        {
            KeyRejected(logger, provider.Id, hint, null);
            return Result<ApiKeyStatusDto>.Failure(AiGatewayErrors.KeyRejected);
        }

        var row = new UserApiKey
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            ProviderId = provider.Id,
            Ciphertext = protector.Protect(key),
            Hint = hint,
            Validation = outcome == KeyCheckOutcome.Accepted ? KeyValidation.Verified : KeyValidation.Unverified,
            UpdatedAt = time.GetUtcNow(),
        };
        await keys.UpsertAsync(row, cancellationToken).ConfigureAwait(false);

        // Yalnızca kalıcı kaydedilen key bilinen sır olur (review r1 M-1): reddedilen ya da biçimsiz değer süreç geneli maskeye girmez.
        SecretMasker.RegisterKnownSecret(key);
        KeySaved(logger, provider.Id, hint, ValidationText(row.Validation), null);
        return Result<ApiKeyStatusDto>.Success(Saved(provider, row));
    }

    /// <inheritdoc />
    public async Task<Result<ApiKeyStatusDto>> DeleteAsync(string providerId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<ApiKeyStatusDto>.Failure(AiGatewayErrors.SignInRequired);
        }

        if (FindProvider(providerId) is not { } provider)
        {
            return Result<ApiKeyStatusDto>.Failure(AiGatewayErrors.UnknownProvider);
        }

        if (await keys.DeleteAsync(userId, provider.Id, cancellationToken).ConfigureAwait(false))
        {
            KeyDeleted(logger, provider.Id, null);
        }

        return Result<ApiKeyStatusDto>.Success(Empty(provider));
    }

    private static string ValidationText(KeyValidation validation) => validation == KeyValidation.Verified ? "verified" : "unverified";

    private static ApiKeyStatusDto Empty(ProviderEntry provider) =>
        new(provider.Id, provider.Name, ApiKeyStatusDto.StateNone, null, null, null);

    private static ApiKeyStatusDto Saved(ProviderEntry provider, UserApiKey row) =>
        new(provider.Id, provider.Name, ApiKeyStatusDto.StateSaved, row.Hint, ValidationText(row.Validation), row.UpdatedAt);

    private ApiKeyStatusDto StatusOf(ProviderEntry provider, UserApiKey row) =>
        protector.CanUnprotect(row.Ciphertext)
            ? Saved(provider, row)
            : new(provider.Id, provider.Name, ApiKeyStatusDto.StateUnreadable, null, null, row.UpdatedAt);

    private ProviderEntry? FindProvider(string? providerId) =>
        catalog.Catalog.Providers.FirstOrDefault(p => string.Equals(p.Id, providerId, StringComparison.Ordinal));
}
