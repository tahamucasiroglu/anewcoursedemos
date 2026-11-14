using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Domain.Entities;

namespace TahaKonsept.Modules.Providers.Application.Base;

/// <summary>
/// Anahtar kasası akışı: kaydı bul → yoksa <c>providers.key_missing</c> → şifreyi çöz → <see cref="ProviderCredential"/>.
/// Somut sınıf kaydı nereden okuyacağını (<see cref="FindAsync"/>) sağlar.
/// </summary>
public abstract class ApiKeyVaultBase : IApiKeyVault
{
    /// <summary>Kurucu.</summary>
    /// <param name="protector">Şifreleyici.</param>
    protected ApiKeyVaultBase(ISecretProtector protector)
    {
        Protector = protector ?? throw new ArgumentNullException(nameof(protector));
    }

    /// <summary>Şifreleyici.</summary>
    protected ISecretProtector Protector { get; }

    /// <inheritdoc />
    public async Task<Result<ProviderCredential>> GetCredentialAsync(Guid userId, string platform, CancellationToken ct)
    {
        var descriptor = ProviderPlatformDirectory.Find(platform);
        if (descriptor is null)
        {
            return Error.Validation(ProviderErrorCodes.UnknownPlatform, $"Bilinmeyen platform: {platform}");
        }

        var entity = await FindAsync(userId, descriptor.Platform, ct);
        if (entity is null)
        {
            return Error.Validation(
                ProviderErrorCodes.KeyMissing,
                $"{descriptor.DisplayName} için API anahtarı girilmemiş. Ayarlar → API anahtarları bölümünden ekleyin.");
        }

        if (!Protector.TryUnprotect(entity.ProtectedKey, out var plain))
        {
            return Error.Failure(
                ProviderErrorCodes.KeyCorrupt,
                $"{descriptor.DisplayName} anahtarı çözülemedi (ana anahtar değişmiş olabilir). Anahtarı yeniden girin.");
        }

        string? baseUrl = null;
        if (entity.ProtectedBaseUrl is not null && !Protector.TryUnprotect(entity.ProtectedBaseUrl, out baseUrl))
        {
            return Error.Failure(
                ProviderErrorCodes.KeyCorrupt,
                $"{descriptor.DisplayName} kök adresi çözülemedi (ana anahtar değişmiş olabilir). Yeniden girin.");
        }

        if (descriptor.RequiresBaseUrl && string.IsNullOrWhiteSpace(baseUrl))
        {
            return Error.Validation(
                ProviderErrorCodes.KeyMissing,
                $"{descriptor.DisplayName} için kök adres girilmemiş. Ayarlar → API anahtarları bölümünden ekleyin.");
        }

        return new ProviderCredential(descriptor.Platform, plain, entity.Region) { BaseUrl = baseUrl };
    }

    /// <summary>Kaydı okur (izlenmeyen).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="platform">Platform (normalize).</param>
    /// <param name="ct">İptal belirteci.</param>
    protected abstract Task<ProviderApiKey?> FindAsync(Guid userId, string platform, CancellationToken ct);
}
