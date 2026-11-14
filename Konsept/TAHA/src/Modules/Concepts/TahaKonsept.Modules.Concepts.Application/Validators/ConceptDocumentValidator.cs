using FluentValidation;
using TahaKonsept.Modules.Concepts.Domain.Constants;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Validators;

/// <summary>
/// <see cref="ConceptDocument"/> doğrulayıcısı (düzenleme ve düzeltme isteklerinde ortak): başlık, 1–60 sahne, sahne metin sınırları.
/// </summary>
public sealed class ConceptDocumentValidator : AbstractValidator<ConceptDocument>
{
    /// <summary>Kuralları tanımlar.</summary>
    public ConceptDocumentValidator()
    {
        RuleFor(x => x.Title).MaximumLength(ConceptLimits.TitleMaxLength).WithMessage($"Başlık en fazla {ConceptLimits.TitleMaxLength} karakter olabilir.");
        RuleFor(x => x.Scenes).NotNull().WithMessage("Sahne listesi boş olamaz.")
            .Must(s => s is { Count: >= 1 and <= ConceptLimits.MaxScenes })
            .WithMessage($"Konseptte 1–{ConceptLimits.MaxScenes} sahne olmalı.");
        RuleForEach(x => x.Scenes).ChildRules(scene =>
        {
            scene.RuleFor(s => s.Title).MaximumLength(ConceptLimits.TitleMaxLength).WithMessage("Sahne başlığı çok uzun.");
            scene.RuleFor(s => s.OnScreen).MaximumLength(ConceptLimits.SceneTextMaxLength).WithMessage("Ekran metni çok uzun.");
            scene.RuleFor(s => s.Narration).MaximumLength(ConceptLimits.SceneTextMaxLength).WithMessage("Seslendirme çok uzun.");
            scene.RuleFor(s => s.VisualPrompt).MaximumLength(ConceptLimits.SceneTextMaxLength).WithMessage("Görsel istemi çok uzun.");
            scene.RuleFor(s => s.DurationHintSec).InclusiveBetween(0, 600).When(s => s.DurationHintSec is not null).WithMessage("Sahne süresi 0–600 sn olmalı.");
        });
    }
}
