namespace Konsept.Modules.Concepts.Domain.Entities;

/// <summary>Model çıktısından ayrıştırılmış konu önerisi adayı; <see cref="HasExtraFields"/> başlık/özet dışı alan (ör. sahneler) varsa.</summary>
/// <param name="Title">Başlık.</param>
/// <param name="Summary">Özet.</param>
/// <param name="HasExtraFields">Başka alan var mı.</param>
public sealed record ParsedTopic(string Title, string Summary, bool HasExtraFields);
