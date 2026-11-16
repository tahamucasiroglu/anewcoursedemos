namespace Konsept.Modules.AiGateway.Application.Dtos;

/// <summary>
/// Çağıran kullanıcı (spec 0013 Req 1, 4): kimliği (yalnızca bu kullanıcının key'i kullanılır, BR-1) ve o anki yetkileri
/// (<c>model.use:…</c>). Yetkiler sunucuda kurulur; istemciden gelmez.
/// </summary>
/// <param name="UserId">Kullanıcı.</param>
/// <param name="Permissions">Yetkiler.</param>
public sealed record AiCaller(Guid UserId, IReadOnlyCollection<string> Permissions);
