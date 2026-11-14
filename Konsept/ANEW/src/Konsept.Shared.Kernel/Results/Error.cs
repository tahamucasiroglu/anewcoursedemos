using Konsept.Shared.Kernel.Enums;

namespace Konsept.Shared.Kernel.Results;

/// <summary>
/// Beklenen bir iş hatası: makinece okunur kod, kullanıcıya gösterilecek Türkçe mesaj ve tür. İsteğe bağlı olarak
/// makinece okunur ek bilgi (<see cref="Metadata"/>, ör. kalan hak) ve alan hataları (<see cref="FieldErrors"/>) taşır;
/// ikisi de gizli bilgi içermez.
/// </summary>
/// <param name="Code">Kararlı hata kodu (ör. <c>agent.not_connected</c>).</param>
/// <param name="Message">Kullanıcıya gösterilecek sade Türkçe mesaj.</param>
/// <param name="Type">HTTP durum kodunu belirleyen hata türü.</param>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>Gets makinece okunur ek bilgi (ör. <c>remainingAttempts</c>, <c>lockedUntil</c>); yoksa <c>null</c>.</summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <summary>Gets alan adına göre doğrulama mesajları; yoksa <c>null</c>.</summary>
    public IReadOnlyDictionary<string, string[]>? FieldErrors { get; init; }
}
