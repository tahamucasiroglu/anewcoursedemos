namespace Konsept.Modules.Profiles.Application.Dtos;

/// <summary>Bir model rolünün seçimi.</summary>
/// <param name="Role">Rol (<c>concept</c> · <c>dedupe</c> · <c>topic</c>).</param>
/// <param name="ModelId">Model (<c>null</c> = seçilmedi).</param>
/// <param name="ModelName">Modelin görünen adı.</param>
/// <param name="Effort">Efor (desteklenmiyorsa <c>null</c>).</param>
public sealed record RoleModelDto(string Role, string? ModelId, string? ModelName, string? Effort);
