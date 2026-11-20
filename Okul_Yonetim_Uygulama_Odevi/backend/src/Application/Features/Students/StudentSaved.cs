namespace OkulYonetim.Application.Features.Students;

/// <summary>
/// Öğrenci değişikliklerinin yanıtı: yalnızca öğrenci kimliği. Kart (sağlık bilgisiyle) ayrıca okunur — böylece her sağlık
/// okuması kart okumasından geçer ve audit'e yazılır.
/// </summary>
public sealed record StudentSaved(Guid Id);
