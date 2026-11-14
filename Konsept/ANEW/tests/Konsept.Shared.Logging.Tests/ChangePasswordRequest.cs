namespace Konsept.Shared.Logging.Tests;

/// <summary>Testte <c>@</c> olmadan (ToString ile) loglanan, şifre taşıyan örnek istek.</summary>
/// <param name="Email">E-posta.</param>
/// <param name="CurrentPassword">Mevcut şifre.</param>
/// <param name="NewPassword">Yeni şifre.</param>
public sealed record ChangePasswordRequest(string Email, string CurrentPassword, string NewPassword);
