using TahaKonsept.Modules.Identity.Domain.Enums;

namespace TahaKonsept.Modules.Identity.Application.Models;

/// <summary>
/// Erişim token'ının içeriği (<see cref="Abstract.IAccessTokenFactory"/> girdisi).
/// </summary>
/// <param name="UserId">Kullanıcı (<c>sub</c>).</param>
/// <param name="UserName">Kullanıcı adı (<c>name</c>).</param>
/// <param name="Roles">Roller (<c>role</c>).</param>
/// <param name="Permissions">İzinler (<c>permission</c>).</param>
/// <param name="FamilyId">Refresh token ailesi (<c>tfam</c>).</param>
/// <param name="Kind">İstemci türü (<c>client</c>: web / agent).</param>
public sealed record AccessTokenDescriptor(
    Guid UserId,
    string UserName,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions,
    Guid FamilyId,
    RefreshTokenKind Kind);
