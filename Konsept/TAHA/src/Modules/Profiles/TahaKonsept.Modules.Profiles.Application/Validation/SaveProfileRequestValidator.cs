using FluentValidation;
using TahaKonsept.Modules.Profiles.Domain.Constants;
using TahaKonsept.Shared.Contracts.Profiles;

namespace TahaKonsept.Modules.Profiles.Application.Validation;

/// <summary>
/// <see cref="SaveProfileRequest"/> doğrulayıcısı (global ValidationFilter çalıştırır). Sınırlar <see cref="ProfileLimits"/>'ten okunur.
/// </summary>
public sealed class SaveProfileRequestValidator : AbstractValidator<SaveProfileRequest>
{
    /// <summary>Kuralları tanımlar.</summary>
    public SaveProfileRequestValidator()
    {
        RuleFor(x => x.Name)
            .Must(v => !string.IsNullOrWhiteSpace(v)).WithMessage("Kanal adı boş olamaz.")
            .MaximumLength(ProfileLimits.NameMaxLength).WithMessage($"Kanal adı en fazla {ProfileLimits.NameMaxLength} karakter olabilir.");

        RuleFor(x => x.Language)
            .NotEmpty().WithMessage("Video dili seçilmeli.")
            .MaximumLength(ProfileLimits.LanguageMaxLength).WithMessage("Dil kodu çok uzun.")
            .Matches("^[A-Za-z]{2,3}(-[A-Za-z0-9]{2,8})?$").WithMessage("Dil kodu geçersiz (ör. tr, en, de).");

        RuleFor(x => x.Format).IsInEnum().WithMessage("Biçim dikey ya da yatay olmalı.");

        RuleFor(x => x.TargetDurationSec)
            .InclusiveBetween(ProfileLimits.MinDurationSec, ProfileLimits.MaxDurationSec)
            .WithMessage($"Hedef süre {ProfileLimits.MinDurationSec}–{ProfileLimits.MaxDurationSec} saniye arasında olmalı.");

        RuleFor(x => x.ChannelDescription)
            .MaximumLength(ProfileLimits.ChannelDescriptionMaxLength)
            .WithMessage($"Kanal içeriği en fazla {ProfileLimits.ChannelDescriptionMaxLength} karakter olabilir.");

        RuleFor(x => x.DefaultTone).MaximumLength(ProfileLimits.StyleFieldMaxLength).WithMessage("Ton çok uzun.");
        RuleFor(x => x.DefaultAudience).MaximumLength(ProfileLimits.StyleFieldMaxLength).WithMessage("Hedef izleyici çok uzun.");
        RuleFor(x => x.DefaultNarrativeStyle).MaximumLength(ProfileLimits.StyleFieldMaxLength).WithMessage("Anlatım biçimi çok uzun.");
        RuleFor(x => x.TemplateKey).MaximumLength(ProfileLimits.TemplateKeyMaxLength).WithMessage("Şablon anahtarı çok uzun.");
    }
}
