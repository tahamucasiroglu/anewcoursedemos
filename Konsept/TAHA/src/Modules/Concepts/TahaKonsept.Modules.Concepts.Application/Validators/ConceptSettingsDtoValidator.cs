using FluentValidation;
using TahaKonsept.Modules.Concepts.Domain.Constants;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Shared.Contracts.Concepts;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Concepts.Application.Validators;

/// <summary>
/// <see cref="ConceptSettingsDto"/> doğrulayıcısı: üç model anahtarı katalogda metin modeli olmalı (OpenRouter için
/// <c>openrouter:&lt;id&gt;</c> da olur), efor geçerli, ince ayar alanları sınır içinde.
/// </summary>
public sealed class ConceptSettingsDtoValidator : AbstractValidator<ConceptSettingsDto>
{
    /// <summary>Kuralları tanımlar.</summary>
    /// <param name="catalog">Model kataloğu.</param>
    public ConceptSettingsDtoValidator(IModelCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        RuleFor(x => x.ConceptModelKey).Must(k => IsTextModel(catalog, k)).WithMessage("Konsept modeli katalogda bir metin modeli olmalı.");
        RuleFor(x => x.TopicModelKey).Must(k => IsTextModel(catalog, k)).WithMessage("Konu önerisi modeli katalogda bir metin modeli olmalı.");
        RuleFor(x => x.WordCheckModelKey).Must(k => IsTextModel(catalog, k)).WithMessage("Kelime denetimi modeli katalogda bir metin modeli olmalı.");
        RuleFor(x => x.ConceptEffort).IsInEnum().When(x => x.ConceptEffort is not null).WithMessage("Efor seviyesi geçersiz.");
        RuleFor(x => x.Tone).MaximumLength(ConceptLimits.StyleMaxLength).WithMessage("Ton çok uzun.");
        RuleFor(x => x.Audience).MaximumLength(ConceptLimits.StyleMaxLength).WithMessage("İzleyici çok uzun.");
        RuleFor(x => x.NarrativeStyle).MaximumLength(ConceptLimits.StyleMaxLength).WithMessage("Anlatım biçimi çok uzun.");
        RuleFor(x => x.Notes).MaximumLength(ConceptLimits.NotesMaxLength).WithMessage($"Notlar en fazla {ConceptLimits.NotesMaxLength} karakter olabilir.");
    }

    private static bool IsTextModel(IModelCatalog catalog, string? key) =>
        !string.IsNullOrWhiteSpace(key) && catalog.Find(key.Trim()) is { Capability: ModelCapability.Text };
}
