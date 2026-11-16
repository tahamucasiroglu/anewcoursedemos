using Konsept.Modules.Concepts.Domain.Entities;

namespace Konsept.Modules.Concepts.Application.Dtos;

/// <summary>Tek konseptin üretim sonucu: konsept ve kelime tekrarı uyarısı ya da kullanıcıya gösterilecek sade hata.</summary>
/// <param name="Concept">Konsept (hatada <c>null</c>).</param>
/// <param name="WordRepetition">Kelime tekrarı uyarısı.</param>
/// <param name="Error">Hata mesajı.</param>
public sealed record GeneratedConcept(ParsedConcept? Concept, bool WordRepetition, string? Error);
