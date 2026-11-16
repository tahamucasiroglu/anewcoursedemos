namespace Konsept.Web.ApiClient.Dtos;

/// <summary>Bir model rolünün seçimi.</summary>
/// <param name="Role">Rol (<c>concept</c> · <c>dedupe</c> · <c>topic</c>).</param>
/// <param name="ModelId">Model (<c>null</c> = seçilmedi).</param>
/// <param name="ModelName">Görünen ad.</param>
/// <param name="Effort">Efor.</param>
public sealed record RoleModelResponse(string Role, string? ModelId, string? ModelName, string? Effort);
