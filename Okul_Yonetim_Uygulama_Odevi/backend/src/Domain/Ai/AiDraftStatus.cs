namespace OkulYonetim.Domain.Ai;

/// <summary>AI taslağının durumu (spec 0012 C-4): insan onayı bekliyor ya da onaylandı (kullanıldı). Vazgeçilen taslak silinir.</summary>
public enum AiDraftStatus
{
    Pending = 1,
    Approved = 2,
}
