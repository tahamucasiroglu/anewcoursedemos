using Microsoft.AspNetCore.Identity;

namespace TahaKonsept.Modules.Identity.Infrastructure.Localization.Concrete;

/// <summary>
/// ASP.NET Core Identity hata mesajlarının Türkçesi (kullanıcıya görünen kurulum/parola hataları).
/// Önemli: <see cref="PasswordTooShort"/>, <see cref="DuplicateUserName"/>, <see cref="InvalidUserName"/>.
/// </summary>
public sealed class TurkishIdentityErrorDescriber : IdentityErrorDescriber
{
    /// <inheritdoc />
    public override IdentityError DefaultError() => Make(nameof(DefaultError), "Bilinmeyen bir hata oluştu.");

    /// <inheritdoc />
    public override IdentityError ConcurrencyFailure() => Make(nameof(ConcurrencyFailure), "Kayıt başka bir işlem tarafından değiştirildi. Lütfen tekrar deneyin.");

    /// <inheritdoc />
    public override IdentityError PasswordMismatch() => Make(nameof(PasswordMismatch), "Parola hatalı.");

    /// <inheritdoc />
    public override IdentityError InvalidToken() => Make(nameof(InvalidToken), "Geçersiz anahtar.");

    /// <inheritdoc />
    public override IdentityError InvalidUserName(string? userName) =>
        Make(nameof(InvalidUserName), $"'{userName}' kullanıcı adı geçersiz; yalnızca harf, rakam ve - . _ @ + kullanılabilir.");

    /// <inheritdoc />
    public override IdentityError DuplicateUserName(string userName) => Make(nameof(DuplicateUserName), $"'{userName}' kullanıcı adı zaten alınmış.");

    /// <inheritdoc />
    public override IdentityError InvalidRoleName(string? role) => Make(nameof(InvalidRoleName), $"'{role}' rol adı geçersiz.");

    /// <inheritdoc />
    public override IdentityError DuplicateRoleName(string role) => Make(nameof(DuplicateRoleName), $"'{role}' rolü zaten var.");

    /// <inheritdoc />
    public override IdentityError UserAlreadyInRole(string role) => Make(nameof(UserAlreadyInRole), $"Kullanıcı zaten '{role}' rolünde.");

    /// <inheritdoc />
    public override IdentityError UserNotInRole(string role) => Make(nameof(UserNotInRole), $"Kullanıcı '{role}' rolünde değil.");

    /// <inheritdoc />
    public override IdentityError UserLockoutNotEnabled() => Make(nameof(UserLockoutNotEnabled), "Bu kullanıcı için kilitleme etkin değil.");

    /// <inheritdoc />
    public override IdentityError PasswordTooShort(int length) => Make(nameof(PasswordTooShort), $"Parola en az {length} karakter olmalı.");

    /// <inheritdoc />
    public override IdentityError PasswordRequiresNonAlphanumeric() => Make(nameof(PasswordRequiresNonAlphanumeric), "Parola en az bir sembol içermeli.");

    /// <inheritdoc />
    public override IdentityError PasswordRequiresDigit() => Make(nameof(PasswordRequiresDigit), "Parola en az bir rakam içermeli.");

    /// <inheritdoc />
    public override IdentityError PasswordRequiresLower() => Make(nameof(PasswordRequiresLower), "Parola en az bir küçük harf içermeli.");

    /// <inheritdoc />
    public override IdentityError PasswordRequiresUpper() => Make(nameof(PasswordRequiresUpper), "Parola en az bir büyük harf içermeli.");

    /// <inheritdoc />
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        Make(nameof(PasswordRequiresUniqueChars), $"Parola en az {uniqueChars} farklı karakter içermeli.");

    private static IdentityError Make(string code, string description) => new() { Code = code, Description = description };
}
