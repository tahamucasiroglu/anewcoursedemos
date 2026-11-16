namespace Konsept.Modules.Concepts.Application.Dtos;

/// <summary>Önerileri sıfırla isteği (spec 0016 Req 9).</summary>
/// <param name="ProfileId">Profil.</param>
public sealed record ResetSuggestionsRequest(Guid? ProfileId);
