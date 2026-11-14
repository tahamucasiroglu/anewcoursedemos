using FluentValidation;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Application.Validators;

/// <summary>
/// <see cref="UserPreferencesDto"/> doğrulaması: tema tanımlı değerlerden biri olmalı.
/// </summary>
public sealed class UserPreferencesDtoValidator : AbstractValidator<UserPreferencesDto>
{
    /// <summary>Kuralları tanımlar.</summary>
    public UserPreferencesDtoValidator()
    {
        RuleFor(x => x.Theme).IsInEnum().WithMessage("Tema açık, koyu ya da sistem olmalı.");
    }
}
