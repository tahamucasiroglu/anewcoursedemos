using FluentValidation;
using TahaKonsept.Modules.Diagnostics.Domain.Entities;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Application.Validation;

/// <summary>
/// <see cref="ActivityLogCreateRequest"/> doğrulaması: mesaj zorunlu (≤ 1000), kaynak ≤ 64, seviye tanımlı, süre ≥ 0, ayrıntı ≤ 32000 karakter.
/// </summary>
public sealed class ActivityLogCreateRequestValidator : AbstractValidator<ActivityLogCreateRequest>
{
    /// <summary>Kuralları tanımlar.</summary>
    public ActivityLogCreateRequestValidator()
    {
        RuleFor(x => x.Message).NotEmpty().WithMessage("Mesaj boş olamaz.")
            .MaximumLength(ActivityLog.MaxMessageLength).WithMessage($"Mesaj en çok {ActivityLog.MaxMessageLength} karakter olabilir.");
        RuleFor(x => x.Source).MaximumLength(ActivityLog.MaxSourceLength).WithMessage($"Kaynak en çok {ActivityLog.MaxSourceLength} karakter olabilir.");
        RuleFor(x => x.Level).IsInEnum().WithMessage("Geçersiz seviye.");
        RuleFor(x => x.DurationMs).GreaterThanOrEqualTo(0).When(x => x.DurationMs.HasValue).WithMessage("Süre negatif olamaz.");
        RuleFor(x => x.Details)
            .Must(d => d is null || d.Value.GetRawText().Length <= ActivityLog.MaxDetailsLength)
            .WithMessage($"Ayrıntı en çok {ActivityLog.MaxDetailsLength} karakter olabilir.");
    }
}
