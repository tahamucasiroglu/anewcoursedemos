using Konsept.Modules.Concepts.Domain.Entities;
using Konsept.Modules.Concepts.Domain.Rules;

namespace Konsept.Modules.Concepts.Application.Dtos;

/// <summary>Üretim işinin tarayıcıya giden görüntüsü (kodlar kararlı metindir; Web resx'le Türkçeleştirir).</summary>
/// <param name="Id">İş.</param>
/// <param name="Lane">Şerit (<c>topic</c> · <c>idea</c>).</param>
/// <param name="Kind">Tür (<c>topics</c> · <c>from_topics</c> · <c>from_idea</c>).</param>
/// <param name="Status">Durum (<c>running</c> · <c>completed</c> · <c>stopped</c> · <c>failed</c>).</param>
/// <param name="Total">Hedef adet.</param>
/// <param name="Processed">İşlenen.</param>
/// <param name="Succeeded">Kaydedilen/listelenen.</param>
/// <param name="FormatRejected">Biçim dışı öneri.</param>
/// <param name="Duplicates">Tekrar.</param>
/// <param name="DedupeUnavailable">Anlamca denetim yapılamadı.</param>
/// <param name="Failures">Üretilemeyenler.</param>
/// <param name="Message">Tamamen başarısız işin mesajı.</param>
public sealed record ConceptJobDto(
    Guid Id,
    string Lane,
    string Kind,
    string Status,
    int Total,
    int Processed,
    int Succeeded,
    int FormatRejected,
    int Duplicates,
    bool DedupeUnavailable,
    IReadOnlyList<JobFailure> Failures,
    string? Message)
{
    /// <summary>Anlık görüntüden.</summary>
    /// <param name="s">Görüntü.</param>
    /// <returns>DTO.</returns>
    public static ConceptJobDto From(ConceptJobSnapshot s)
    {
        ArgumentNullException.ThrowIfNull(s);
        return new(
            s.Id,
            ConceptCodes.Of(s.Lane),
            ConceptCodes.Of(s.Kind),
            ConceptCodes.Of(s.Status),
            s.Total,
            s.Processed,
            s.Succeeded,
            s.FormatRejected,
            s.Duplicates,
            s.DedupeUnavailable,
            s.Failures,
            s.Message);
    }
}
