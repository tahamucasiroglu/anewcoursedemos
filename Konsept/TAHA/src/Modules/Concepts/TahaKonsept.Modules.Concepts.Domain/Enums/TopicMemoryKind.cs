namespace TahaKonsept.Modules.Concepts.Domain.Enums;

/// <summary>
/// Konu hafızası kaydının türü. Sayısal değer "güç" sırasıdır: daha güçlü tür zayıfın yerine geçer
/// (<see cref="Suggested"/> &lt; <see cref="Generated"/> &lt; <see cref="Disliked"/> &lt; <see cref="Deleted"/>).
/// <see cref="Disliked"/> ve <see cref="Deleted"/> prompta negatif örnek olarak girer.
/// </summary>
public enum TopicMemoryKind
{
    /// <summary>Önerildi (seçilmedi).</summary>
    Suggested = 0,

    /// <summary>Konsepti üretildi.</summary>
    Generated = 1,

    /// <summary>Kullanıcı beğenmedi.</summary>
    Disliked = 2,

    /// <summary>Konsepti silindi.</summary>
    Deleted = 3,
}
