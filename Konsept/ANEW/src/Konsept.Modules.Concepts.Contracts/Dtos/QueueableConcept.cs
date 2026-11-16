using Konsept.Modules.Concepts.Contracts.Enums;

namespace Konsept.Modules.Concepts.Contracts.Dtos;

/// <summary>Üretim sırasının bir konsept hakkında bildiği (spec 0006 Req 10, 20): kayıttaki dil/biçim/süre, profil değil.</summary>
/// <param name="Id">Konsept.</param>
/// <param name="ProfileId">Profil.</param>
/// <param name="Title">Başlık.</param>
/// <param name="Stage">Durum.</param>
/// <param name="Language">Kayıttaki video dili.</param>
/// <param name="Format">Kayıttaki biçim.</param>
/// <param name="TargetDurationSeconds">Kayıttaki hedef süre.</param>
public sealed record QueueableConcept(
    Guid Id, Guid ProfileId, string Title, ConceptQueueStage Stage, string Language, string Format, int TargetDurationSeconds);
