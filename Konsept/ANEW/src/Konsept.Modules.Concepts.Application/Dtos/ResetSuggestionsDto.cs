namespace Konsept.Modules.Concepts.Application.Dtos;

/// <summary>Önerileri sıfırla sonucu.</summary>
/// <param name="ProfileId">Profil.</param>
/// <param name="Cleared">Hafızadan silinen önerilip seçilmemiş başlık sayısı.</param>
public sealed record ResetSuggestionsDto(Guid ProfileId, int Cleared);
