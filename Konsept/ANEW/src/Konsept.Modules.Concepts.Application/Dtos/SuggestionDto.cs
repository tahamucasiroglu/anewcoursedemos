namespace Konsept.Modules.Concepts.Application.Dtos;

/// <summary>Bekleyen konu önerisi.</summary>
/// <param name="Id">Kimlik.</param>
/// <param name="Title">Başlık.</param>
/// <param name="Summary">Kısa özet.</param>
public sealed record SuggestionDto(Guid Id, string Title, string Summary);
