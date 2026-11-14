namespace Konsept.Web.ApiClient.Dtos;

/// <summary>Geçerli oturumun kullanıcısı.</summary>
/// <param name="UserId">Kullanıcı.</param>
/// <param name="Name">Ad.</param>
/// <param name="Email">E-posta.</param>
public sealed record CurrentUserResponse(Guid UserId, string Name, string Email);
