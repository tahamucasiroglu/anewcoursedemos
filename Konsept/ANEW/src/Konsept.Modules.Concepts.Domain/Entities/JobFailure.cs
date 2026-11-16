namespace Konsept.Modules.Concepts.Domain.Entities;

/// <summary>Üretilemeyen bir konsept (spec 0014 Req 17): etiket (konudan üretimde konu başlığı, fikirden üretimde sıra numarası) ve sade mesaj.</summary>
/// <param name="Label">Etiket.</param>
/// <param name="Message">Türkçe, sade mesaj (stack trace ya da key yok).</param>
public sealed record JobFailure(string Label, string Message);
