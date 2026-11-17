namespace Konsept.Modules.Identity.Application.Dtos;

/// <summary>
/// Admin kullanıcı satırı (spec 0024 Req 1, 7, 8). Konsept, profil, API key ya da harcama içermez (BR-1).
/// </summary>
/// <param name="Id">Kullanıcı.</param>
/// <param name="Name">Ad.</param>
/// <param name="Email">E-posta.</param>
/// <param name="RegisteredAt">Kayıt anı (UTC).</param>
/// <param name="LastLoginAt">Son giriş (UTC).</param>
/// <param name="Status"><c>active</c> | <c>banned</c>.</param>
/// <param name="Role"><c>user</c> | <c>admin</c>.</param>
/// <param name="Clips">Ürettiği klip sayısı.</param>
/// <param name="IsSelf">İsteği yapan admin mi.</param>
/// <param name="History">Ban kayıtları, en yeni üstte.</param>
public sealed record AdminUserDto(
    Guid Id,
    string Name,
    string Email,
    DateTimeOffset RegisteredAt,
    DateTimeOffset? LastLoginAt,
    string Status,
    string Role,
    int Clips,
    bool IsSelf,
    IReadOnlyList<AdminBanRecordDto> History);
