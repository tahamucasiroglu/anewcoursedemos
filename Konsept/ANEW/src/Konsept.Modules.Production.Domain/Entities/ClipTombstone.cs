namespace Konsept.Modules.Production.Domain.Entities;

/// <summary>
/// Kalıcı silinen videonun klip izi (spec 0019, B-130 → spec 0010 Req 3): admin klip sayısı kalıcı silmeyle düşmesin diye yalnızca konsept
/// kimliği ve ilk tamamlanma anı tutulur. İçerik (başlık, metin, dosya adı, kullanıcı) yoktur.
/// </summary>
public sealed record ClipTombstone
{
    /// <summary>Gets konsept (video) kimliği.</summary>
    public Guid ConceptId { get; init; }

    /// <summary>Gets ilk tamamlanan sürümün bitişi (UTC).</summary>
    public DateTimeOffset FirstCompletedAt { get; init; }
}
