using TahaKonsept.Modules.Concepts.Domain.Enums;

namespace TahaKonsept.Modules.Concepts.Application.Models;

/// <summary>
/// Konu hafızası anlık görüntüsündeki tek kayıt: başlık, tür, parmak izi ve son güncellenme.
/// </summary>
/// <param name="Title">Başlık.</param>
/// <param name="Kind">Tür.</param>
/// <param name="Fingerprint">Parmak izi.</param>
/// <param name="UpdatedAtUtc">Son güncellenme.</param>
public sealed record TopicMemoryItem(string Title, TopicMemoryKind Kind, TopicFingerprint Fingerprint, DateTimeOffset UpdatedAtUtc)
{
    /// <summary>Negatif örnek mi.</summary>
    public bool IsNegative => Kind is TopicMemoryKind.Deleted or TopicMemoryKind.Disliked;
}
