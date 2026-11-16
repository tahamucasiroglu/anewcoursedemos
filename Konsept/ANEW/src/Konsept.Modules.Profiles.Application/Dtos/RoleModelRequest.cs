namespace Konsept.Modules.Profiles.Application.Dtos;

/// <summary>Bir rol için model ve efor seçimi.</summary>
/// <param name="Role">Rol.</param>
/// <param name="ModelId">Model (<c>null</c> = seçilmedi).</param>
/// <param name="Effort">Efor (boş = katalog varsayılanı).</param>
public sealed record RoleModelRequest(string? Role, string? ModelId, string? Effort);
