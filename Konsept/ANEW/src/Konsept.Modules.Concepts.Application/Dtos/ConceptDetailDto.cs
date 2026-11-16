using Konsept.Modules.Concepts.Domain.Entities;

namespace Konsept.Modules.Concepts.Application.Dtos;

/// <summary>
/// Konseptin tamamı (spec 0014 Req 23, 27): başlık, durum, kendi taşıdığı profil değerleri, sahneler ve uyarılar. Metin düz veridir;
/// istemci HTML olarak yorumlamaz.
/// </summary>
/// <param name="Id">Kimlik.</param>
/// <param name="ProfileId">Profil.</param>
/// <param name="Title">Başlık.</param>
/// <param name="Status">Durum kodu.</param>
/// <param name="Language">Video dili.</param>
/// <param name="Format">Biçim.</param>
/// <param name="TargetDurationSeconds">Hedef süre.</param>
/// <param name="Scenes">Sahneler.</param>
/// <param name="WordRepetitionWarning">Kelime tekrarı uyarısı.</param>
/// <param name="DurationMismatchWarning">Süre uyumsuz uyarısı.</param>
/// <param name="CreatedAt">Oluşturulma (UTC).</param>
public sealed record ConceptDetailDto(
    Guid Id,
    Guid ProfileId,
    string Title,
    string Status,
    string Language,
    string Format,
    int TargetDurationSeconds,
    IReadOnlyList<ConceptScene> Scenes,
    bool WordRepetitionWarning,
    bool DurationMismatchWarning,
    DateTimeOffset CreatedAt);
