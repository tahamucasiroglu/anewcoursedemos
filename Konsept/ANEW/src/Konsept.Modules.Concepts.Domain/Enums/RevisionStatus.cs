namespace Konsept.Modules.Concepts.Domain.Enums;

/// <summary>Konsept revizyonunun durumu (spec 0015 Req 8, 12).</summary>
public enum RevisionStatus
{
    /// <summary>Yeniden üretiliyor.</summary>
    Running = 0,

    /// <summary>Revizyon hazır (modalda gösterilir).</summary>
    Ready = 1,

    /// <summary>Üretilemedi (sağlayıcı hatası, yarım kaldı ya da biçim bozuk); revizyon yok.</summary>
    Failed = 2,
}
