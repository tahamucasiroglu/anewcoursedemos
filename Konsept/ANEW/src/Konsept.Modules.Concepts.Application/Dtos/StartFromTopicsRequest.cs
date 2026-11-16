namespace Konsept.Modules.Concepts.Application.Dtos;

/// <summary>Seçtiklerini üret isteği.</summary>
/// <param name="ProfileId">Profil.</param>
/// <param name="SuggestionIds">Seçilen bekleyen öneriler.</param>
public sealed record StartFromTopicsRequest(Guid? ProfileId, IReadOnlyList<Guid>? SuggestionIds);
