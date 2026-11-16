using Konsept.Modules.Concepts.Domain.Entities;
using Konsept.Modules.Concepts.Domain.Rules;

namespace Konsept.Modules.Concepts.Application.Dtos;

/// <summary>Konseptin revizyon durumu (spec 0015 Req 8): yok, yeniden üretiliyor, hazır (sahneler + uyarılar) ya da başarısız (sade hata).</summary>
/// <param name="ConceptId">Konsept.</param>
/// <param name="Id">Revizyon (yoksa <c>null</c>).</param>
/// <param name="Status">Durum kodu: <c>none</c> · <c>running</c> · <c>ready</c> · <c>failed</c>.</param>
/// <param name="Scenes">Sahneler (hazırken).</param>
/// <param name="WordRepetitionWarning">Kelime tekrarı uyarısı.</param>
/// <param name="DurationMismatchWarning">Süre uyumsuz uyarısı.</param>
/// <param name="Error">Hata mesajı.</param>
public sealed record RevisionDto(
    Guid ConceptId,
    Guid? Id,
    string Status,
    IReadOnlyList<ConceptScene> Scenes,
    bool WordRepetitionWarning,
    bool DurationMismatchWarning,
    string? Error)
{
    /// <summary>Revizyon yok.</summary>
    /// <param name="conceptId">Konsept.</param>
    /// <returns>Durum.</returns>
    public static RevisionDto None(Guid conceptId) => new(conceptId, null, "none", [], false, false, null);

    /// <summary>Anlık görüntüden.</summary>
    /// <param name="s">Görüntü.</param>
    /// <returns>Durum.</returns>
    public static RevisionDto From(ConceptRevisionSnapshot s)
    {
        ArgumentNullException.ThrowIfNull(s);
        return new(s.ConceptId, s.Id, ConceptCodes.Of(s.Status), s.Scenes, s.WordRepetitionWarning, s.DurationMismatchWarning, s.Error);
    }
}
