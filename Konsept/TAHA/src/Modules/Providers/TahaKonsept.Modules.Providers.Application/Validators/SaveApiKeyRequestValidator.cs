using FluentValidation;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Validators;

/// <summary>
/// <see cref="SaveApiKeyRequest"/> doğrulaması: bilinen platform, 8-512 karakter boşluksuz anahtar, Azure Speech için bölge, Yerel LLM için kök adres (anahtar isteğe bağlı).
/// </summary>
public sealed class SaveApiKeyRequestValidator : AbstractValidator<SaveApiKeyRequest>
{
    /// <summary>Kuralları tanımlar.</summary>
    public SaveApiKeyRequestValidator()
    {
        RuleFor(x => x.Platform)
            .NotEmpty().WithMessage("Platform gerekli.")
            .Must(p => ProviderPlatformDirectory.Find(p) is not null).WithMessage("Bilinmeyen platform.");
        RuleFor(x => x.ApiKey)
            .NotEmpty().WithMessage("Anahtar gerekli.")
            .Must(k => k.Trim().Length is >= 8 and <= 512).WithMessage("Anahtar 8-512 karakter olmalı.")
            .Must(k => !k.Trim().Any(char.IsWhiteSpace)).WithMessage("Anahtar boşluk içeremez.")
            .When(x => ProviderPlatformDirectory.Find(x.Platform)?.KeyOptional != true);
        RuleFor(x => x.ApiKey)
            .Must(k => k is null || k.Trim().Length <= 512).WithMessage("Anahtar en fazla 512 karakter olabilir.")
            .Must(k => k is null || !k.Trim().Any(char.IsWhiteSpace)).WithMessage("Anahtar boşluk içeremez.")
            .When(x => ProviderPlatformDirectory.Find(x.Platform)?.KeyOptional == true);
        RuleFor(x => x.BaseUrl)
            .Must(u => BaseUrlNormalizer.Normalize(u) is not null)
            .WithMessage("Geçerli bir kök adres girin (http:// veya https://, ör. http://localhost:11434/v1).")
            .When(x => ProviderPlatformDirectory.Find(x.Platform)?.RequiresBaseUrl == true);
        RuleFor(x => x.Region)
            .NotEmpty().WithMessage("Azure Speech için bölge gerekli (ör. westeurope).")
            .Matches("^[a-zA-Z0-9-]{2,40}$").WithMessage("Bölge yalnızca harf, rakam ve tire içerebilir.")
            .When(x => ProviderPlatformDirectory.Find(x.Platform)?.RequiresRegion == true);
    }
}
