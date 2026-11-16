using Konsept.Modules.Concepts.Domain.Enums;

namespace Konsept.Modules.Concepts.Domain.Entities;

/// <summary>Üretim işinin anlık görüntüsü (ilerleme "k/N", sonuç sayıları ve bilgiler; spec 0014 Req 3, 6, 9, 17, 19).</summary>
/// <param name="Id">İş.</param>
/// <param name="ProfileId">Profil.</param>
/// <param name="Lane">Şerit.</param>
/// <param name="Kind">Tür.</param>
/// <param name="Status">Durum.</param>
/// <param name="Total">Hedef adet.</param>
/// <param name="Processed">İşlenen (başarılı + hatalı).</param>
/// <param name="Succeeded">Kaydedilen konsept ya da listelenen öneri.</param>
/// <param name="FormatRejected">Biçim dışı öneri.</param>
/// <param name="Duplicates">Mevcut konularla aynı olduğu için kaydedilmeyen/listelenmeyen.</param>
/// <param name="DedupeUnavailable">Anlamca denetim yapılamadı.</param>
/// <param name="Failures">Üretilemeyenler.</param>
/// <param name="Message">Tamamen başarısız işin sade mesajı.</param>
public sealed record ConceptJobSnapshot(
    Guid Id,
    Guid ProfileId,
    JobLane Lane,
    JobKind Kind,
    JobStatus Status,
    int Total,
    int Processed,
    int Succeeded,
    int FormatRejected,
    int Duplicates,
    bool DedupeUnavailable,
    IReadOnlyList<JobFailure> Failures,
    string? Message);
