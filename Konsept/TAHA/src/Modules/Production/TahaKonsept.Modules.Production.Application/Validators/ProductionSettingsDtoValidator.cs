using FluentValidation;
using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Validators;

/// <summary>
/// <see cref="ProductionSettingsDto"/> doğrulayıcısı: kod modeli zorunlu, motor bilinen biri, fps 15-60, hız 0.5-2;
/// görsel/ses açıksa model seçilmiş olmalı. Mesajlar Türkçe.
/// </summary>
public sealed class ProductionSettingsDtoValidator : AbstractValidator<ProductionSettingsDto>
{
    /// <summary>Kuralları kurar.</summary>
    public ProductionSettingsDtoValidator()
    {
        RuleFor(x => x.CodeModelKey).NotEmpty().WithMessage("Video kodu modeli seçilmeli.").MaximumLength(200);
        RuleFor(x => x.VideoEngine)
            .Must(e => PipelineStages.KnownEngines.Contains((e ?? string.Empty).Trim().ToLowerInvariant()))
            .WithMessage("Video motoru hyperframes, remotion ya da revideo olmalı.");
        RuleFor(x => x.Fps).InclusiveBetween(15, 60).WithMessage("Kare hızı 15-60 arasında olmalı.");
        RuleFor(x => x.SpeechSpeed).InclusiveBetween(0.5, 2.0).WithMessage("Konuşma hızı 0.5-2.0 arasında olmalı.");
        RuleFor(x => x.ImageModelKey).NotEmpty().When(x => x.ImageMode != MediaMode.Off).WithMessage("Görsel açıkken görsel modeli seçilmeli.");
        RuleFor(x => x.SpeechModelKey).NotEmpty().When(x => x.SpeechMode != MediaMode.Off).WithMessage("Ses açıkken ses modeli seçilmeli.");
        RuleFor(x => x.VoiceId).MaximumLength(200);
    }
}
