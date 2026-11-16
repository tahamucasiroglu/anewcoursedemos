namespace Konsept.Modules.AiGateway.Contracts.Dtos;

/// <summary>
/// Çağıran kullanıcı ve o anki model yetkileri; isteğin içinde sunucuda yakalanır (istemciden gelmez) ve arka plan işine taşınır
/// (spec 0014 Req 20). Yalnızca bu kullanıcının key'i kullanılır (BR-1).
/// </summary>
/// <param name="UserId">Kullanıcı.</param>
/// <param name="Permissions">Yetkiler (<c>model.use:…</c>).</param>
public sealed record ModelCallerIdentity(Guid UserId, IReadOnlyCollection<string> Permissions);
