namespace TahaKonsept.Modules.Providers.Application.Models;

/// <summary>
/// Bir sağlayıcı çağrısının kime ve hangi amaçla yapıldığı (CallLog satırının kimlik kısmı).
/// </summary>
/// <param name="UserId">Kullanıcı.</param>
/// <param name="Purpose">Amaç (ör. <c>concept</c>, <c>scene-code</c>, <c>topics</c>).</param>
public sealed record CallContext(Guid UserId, string Purpose);
