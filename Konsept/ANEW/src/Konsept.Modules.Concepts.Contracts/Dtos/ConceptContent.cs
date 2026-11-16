namespace Konsept.Modules.Concepts.Contracts.Dtos;

/// <summary>
/// Üretim hattının okuduğu konsept metni (spec 0017 Req 7, 10, 22): başlık, kayıttaki dil/biçim ve sıralı sahneler. Metnin özeti çıktıların
/// yeniden kullanımına, kopyası sürüm kaydına girer.
/// </summary>
/// <param name="Id">Konsept.</param>
/// <param name="ProfileId">Profil.</param>
/// <param name="Title">Başlık.</param>
/// <param name="Language">Kayıttaki video dili.</param>
/// <param name="Format">Kayıttaki biçim.</param>
/// <param name="Scenes">Sahneler (sıralı).</param>
public sealed record ConceptContent(Guid Id, Guid ProfileId, string Title, string Language, string Format, IReadOnlyList<ConceptSceneContent> Scenes);
