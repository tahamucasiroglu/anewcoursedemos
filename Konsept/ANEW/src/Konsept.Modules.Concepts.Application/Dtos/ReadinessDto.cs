namespace Konsept.Modules.Concepts.Application.Dtos;

/// <summary>
/// Üret düğmelerinin hazır olması (spec 0014 Req 1): Konu üret → konu önerisi + anlamca tekrar denetimi; Seçtiklerini üret → konsept;
/// fikirden Üret → konsept + anlamca tekrar denetimi. Rol için model seçilmiş ve seçilebilir (yetki + geçerli key) olmalı.
/// </summary>
/// <param name="Topics">Konu üret.</param>
/// <param name="FromTopics">Seçtiklerini üret.</param>
/// <param name="Idea">Fikirden üret.</param>
/// <param name="MissingRoles">Hazır olmayan roller (<c>concept</c>, <c>dedupe</c>, <c>topic</c>).</param>
public sealed record ReadinessDto(bool Topics, bool FromTopics, bool Idea, IReadOnlyList<string> MissingRoles);
