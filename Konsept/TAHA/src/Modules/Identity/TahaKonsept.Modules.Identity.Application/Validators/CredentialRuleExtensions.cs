using FluentValidation;

namespace TahaKonsept.Modules.Identity.Application.Validators;

/// <summary>
/// Kayıt ve kurulumda ortak kimlik bilgisi kuralları (tek yer): <see cref="ValidUserName{T}"/> (boş değil, kırpılmış 3-64 karakter)
/// ve <see cref="ValidNewPassword{T}"/> (8-128 karakter). Identity seçenekleri de en az 8 karakter ister.
/// </summary>
public static class CredentialRuleExtensions
{
    /// <summary>Kullanıcı adı kuralı.</summary>
    /// <typeparam name="T">Doğrulanan tür.</typeparam>
    /// <param name="rule">Kural oluşturucu.</param>
    /// <returns>Kural.</returns>
    public static IRuleBuilderOptions<T, string> ValidUserName<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .Must(v => !string.IsNullOrWhiteSpace(v)).WithMessage("Kullanıcı adı gerekli.")
            .Must(v => (v ?? string.Empty).Trim().Length is >= 3 and <= 64).WithMessage("Kullanıcı adı 3-64 karakter olmalı.");

    /// <summary>Yeni parola kuralı.</summary>
    /// <typeparam name="T">Doğrulanan tür.</typeparam>
    /// <param name="rule">Kural oluşturucu.</param>
    /// <returns>Kural.</returns>
    public static IRuleBuilderOptions<T, string> ValidNewPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Parola gerekli.")
            .Length(8, 128).WithMessage("Parola 8-128 karakter olmalı.");
}
